#nullable enable

namespace SKD.Service;

public class BomService {
    private readonly SkdContext context;

    public BomService(SkdContext ctx) {
        this.context = ctx;
    }

    ///<summary>
    /// Import BOM lot, kits, lot parts from bom file
    ///</summary>
    public async Task<MutationResult<BomOverviewDTO>> ImportBom(BomFile input) {
        MutationResult<BomOverviewDTO> result = new();
        result.Errors = await ValidateBomFileInput<BomFile>(input);
        if (result.Errors.Count > 0) {
            return result;
        }

        // possible re-import of the same BOM file, ignore existing     
        input = await RemoveEntriesForAlreadyImportedLots(input); 

        // add BOM
        var bom = await GetEnsureBom(plantCode: input.PlantCode, sequence: input.Sequence);

        // add parts
        var parts = await GetEnsureParts(input);

        // add Lots
        await EnsureLots(input, bom);

        // add Kits
        await EnsureKits(input, bom);

        // add Lot Part
        Ensure_LotParts(input, bom, parts);

        await context.SaveChangesAsync();
        result.Payload = await GetBomOverview(bom.Sequence);
        return result;
    }

    #region import bom lot helpers

    private async Task<Bom> GetEnsureBom(string plantCode, int sequence) {
        var plant = await context.Plants.FirstOrDefaultAsync(t => t.Code == plantCode);
        var bom = await context.Boms
            .Include(t => t.Plant)
            .Include(t => t.Lots).ThenInclude(t => t.Kits)
            .FirstOrDefaultAsync(t => t.Plant.Code == plantCode && t.Sequence == sequence);

        if (bom == null) {
            bom = new Bom {
                Plant = plant,
                Sequence = sequence
            };
            context.Boms.Add(bom);
        }
        return bom;
    }

    private async Task EnsureLots(BomFile input, Bom bom) {
        var plant = await context.Plants.FirstOrDefaultAsync(t => t.Code == input.PlantCode);
        foreach (var lotNo in input.LotEntries.Select(t => t.LotNo)) {
            var lot = bom.Lots.FirstOrDefault(t => t.LotNo == lotNo);
            if (lot == null) {
                var pcvCode = lotNo.Substring(0, EntityFieldLen.Pcv_Code);
                lot = new Lot {
                    LotNo = lotNo,
                    Plant = plant,
                    Pcv = await context.Pcvs.FirstOrDefaultAsync(t => t.Code == pcvCode)
                };
                bom.Lots.Add(lot);
            }
        }
    }

    private async Task<List<Part>> GetEnsureParts(BomFile input) {
        // Reformat to ensure consistent part number format
        input.LotParts.ToList().ForEach(t => {
            t.PartNo = PartService.ReFormatPartNo(t.PartNo);
        });

        var partService = new PartService(context);
        List<(string, string)> inputParts = input.LotParts
            .Select(t => (t.PartNo, t.PartDesc)).ToList();
        return await partService.GetEnsureParts(inputParts);
    }

    private void Ensure_LotParts(
        BomFile input,
        Bom bom,
        IEnumerable<Part> parts
    ) {

        foreach (var lotGroup in input.LotParts.GroupBy(t => t.LotNo)) {
            var lot = bom.Lots.First(t => t.LotNo == lotGroup.Key);
            
            foreach (var inputLotPart in lotGroup) {
                var newLotPart = new LotPart {
                    Part = parts.First(t => t.PartNo == inputLotPart.PartNo),
                    BomQuantity = inputLotPart.Quantity
                };
                lot.LotParts.Add(newLotPart);
            }
        }
    }

    #endregion

    public async Task<List<Error>> ValidateVehicleLotPartsInput<T>(BomLotPartDTO input) where T : BomLotPartDTO {
        var errors = new List<Error>();

        var plant = await context.Plants.FirstOrDefaultAsync(t => t.Code == input.PlantCode);
        if (plant == null) {
            errors.Add(new Error("PlantCode", $"plant not found  {input.PlantCode}"));
            return errors;
        }

        if (!input.LotParts.Any()) {
            errors.Add(new Error("", "No lot parts found"));
            return errors;
        }

        // duplicate lotNo + Part in payload
        var duplicateLotParts = input.LotParts.GroupBy(t => new { t.LotNo, t.PartNo })
            .Any(g => g.Count() > 1);
        if (duplicateLotParts) {
            errors.Add(new Error("", "Duplicate Lot + Part number(s) in payload"));
            return errors;
        }

        // validate lotNo format
        if (input.LotParts.Any(t => !Validator.Valid_LotNo(t.LotNo))) {
            errors.Add(new Error("", "Lot numbers with invalid format found"));
            return errors;
        }

        if (input.LotParts.Any(t => t.PartNo is null or "")) {
            errors.Add(new Error("", "entries with missing part number(s)"));
            return errors;
        }

        if (input.LotParts.Any(t => t.PartDesc is null or "")) {
            errors.Add(new Error("", "entries with missing part decription(s)"));
            return errors;
        }

        if (input.LotParts.Any(t => t.Quantity <= 0)) {
            errors.Add(new Error("", "entries with quantity <= 0"));
            return errors;
        }

        return errors;
    }

    private async Task EnsureKits(BomFile input, Bom bom) {
        foreach (var inputLot in input.LotEntries) {
            var pcvCode = inputLot.Kits.Select(t => t.PcvCode).First();
            var lot = bom.Lots.First(t => t.LotNo == inputLot.LotNo);
            // kits could have been previously imported.
            if (!lot.Kits.Any()) {
                foreach (var inputKit in inputLot.Kits) {
                    var kit = await CreateKit(inputKit);
                    lot.Kits.Add(kit);
                }
            }
        }
    }

    private async Task<Kit> CreateKit(BomFile.BomFileLot.BomFileKit input) {
        var kits = new List<Kit>();

        var pcv = await context.Pcvs
            .Include(t => t.PcvComponents)
            .Where(t => t.Code == input.PcvCode)
            .FirstAsync();

        var kit = new Kit {
            KitNo = input.KitNo
        };

        pcv.PcvComponents.ToList().ForEach(mapping => {
            kit.KitComponents.Add(new KitComponent() {
                ComponentId = mapping.ComponentId,
                ProductionStationId = mapping.ProductionStationId,
                CreatedAt = kit.CreatedAt
            });
        });

        return kit;
    }

    public async Task<List<Error>> ValidateBomFileInput<T>(BomFile input) where T : BomFile {
        var errors = new List<Error>();

        var plant = await context.Plants.FirstOrDefaultAsync(t => t.Code == input.PlantCode);
        if (plant == null) {
            errors.Add(new Error("PlantCode", $"plant not found  {input.PlantCode}"));
            return errors;
        }

        if (!input.LotEntries.Any()) {
            errors.Add(new Error("", "no lots found"));
            return errors;
        }

        if (!input.LotParts.Any()) {
            errors.Add(new Error("", "No lot parts found"));
            return errors;
        }

        // kits alread imported
        var newLotNumbers = input.LotEntries.Select(t => t.LotNo).ToList();
        var previouslyImportedLotNumbers = await context.Lots
            .Where(t => newLotNumbers.Any(newLotNo => newLotNo == t.LotNo))
            .Select(t => t.LotNo)
            .ToListAsync();

        if (previouslyImportedLotNumbers.Count() == input.LotEntries.Count()) {
            errors.Add(new Error("", "Lots already imported"));
        }

        // duplicate lot number in lot enties
        var duplicate_lotNo = input.LotEntries.GroupBy(t => t.LotNo)
            .Any(g => g.Count() > 1);
        if (duplicate_lotNo) {
            errors.Add(new Error("", "duplicate Lot numbers in payload"));
            return errors;
        }

        // duplicate lot part number
        var duplicate_lotPart = input.LotParts.GroupBy(t => new { t.LotNo, t.PartNo })
            .Any(g => g.Count() > 1);
        if (duplicate_lotPart) {
            errors.Add(new Error("", "duplicate Lot + Part number(s) in payload"));
            return errors;
        }

        // validate lotNo format
        if (input.LotEntries.Any(t => !Validator.Valid_LotNo(t.LotNo))) {
            errors.Add(new Error("", "lot numbers  with invalid format found"));
            return errors;
        }

        // validate kitNo format
        if (input.LotEntries.Any(t => t.Kits.Any(k => !Validator.Valid_KitNo(k.KitNo)))) {
            errors.Add(new Error("", "kit numbers with invalid format found"));
            return errors;
        }

        // missing pcv code
        if (input.LotEntries.Any(t => t.Kits.Any(k => k.PcvCode is null or ""))) {
            errors.Add(new Error("", "kits with missing pcv code found"));
            return errors;
        }

        // pcv codes not found
        var incommingPcvCodes = input.LotEntries.SelectMany(t => t.Kits).Select(k => k.PcvCode).Distinct();
        var systemPcvCodes = await context.Pcvs
            .Where(t => t.RemovedAt == null).Select(t => t.Code).ToListAsync();

        var matchingModelCodes = incommingPcvCodes.Intersect(systemPcvCodes);
        var missingModelCodes = incommingPcvCodes.Except(matchingModelCodes);

        if (missingModelCodes.Any()) {
            errors.Add(new Error("", $"pcv codes not in system or removed: {String.Join(",", missingModelCodes)}"));
            return errors;
        }

        return errors;
    }


    public async Task<BomOverviewDTO> GetBomOverview(Guid id) {
        var bom = await context.Boms
            .Where(t => t.Id == id)
            .Select(t => new BomOverviewDTO {
                Id = t.Id,
                PlantCode = t.Plant.Code,
                Sequence = t.Sequence,
                Shipments = t.Lots.SelectMany(u => u.ShipmentLots).Select(u => new BomShipInfoDTO {
                    ShipmentId = u.ShipmentId,
                    Sequence = u.Shipment.Sequence,
                    PlantCode = t.Plant.Code
                }).ToList(),
                LotCount = t.Lots.Count,
                PartCount = t.Lots.SelectMany(u => u.LotParts).Select(u => u.Part).Distinct().Count(),
                VehicleCount = t.Lots.SelectMany(u => u.Kits).Count(),
                CreatedAt = t.CreatedAt
            })
            .FirstAsync();

        return bom;
    }

    public async Task<BomOverviewDTO> GetBomOverview(int bomSequenceNo) {
        var bom = await context.Boms
            .Where(t => t.Sequence == bomSequenceNo)
            .Select(t => new BomOverviewDTO {
                Id = t.Id,
                PlantCode = t.Plant.Code,
                Sequence = t.Sequence,
                // ShipmentSequences = t.Lots.SelectMany(t => t.ShipmentLots).Select(u => u.Shipment).Select(u => u.Sequence).ToList(),
                LotCount = t.Lots.Count,
                PartCount = t.Lots.SelectMany(u => u.LotParts).Select(u => u.Part).Distinct().Count(),
                VehicleCount = t.Lots.SelectMany(u => u.Kits).Count(),
                CreatedAt = t.CreatedAt
            })
            .FirstAsync();

        return bom;
    }

    public async Task<BomOverviewDTO> GetBomLots(Guid id) {
        var bom = await context.Boms
            .Where(t => t.Id == id)
            .Select(t => new BomOverviewDTO {
                Id = t.Id,
                PlantCode = t.Plant.Code,
                Sequence = t.Sequence,
                LotCount = t.Lots.Count,
                PartCount = t.Lots.SelectMany(u => u.LotParts).Select(u => u.Part).Distinct().Count(),
                VehicleCount = t.Lots.SelectMany(u => u.Kits).Count(),
                CreatedAt = t.CreatedAt
            })
            .FirstAsync();

        return bom;
    }

    private async Task<BomFile> RemoveEntriesForAlreadyImportedLots(BomFile input) {
        var bomLotNumbers = input.LotEntries.Select(t => t.LotNo).ToList();
        var existingLotNumbers = await context.Lots
            .Where(t => bomLotNumbers.Any(ln => ln == t.LotNo))
            .Select(t => t.LotNo).ToListAsync();

        input.LotEntries = input.LotEntries.Where(t => !existingLotNumbers.Any(existingLotNo => existingLotNo == t.LotNo)).ToList();
        input.LotParts = input.LotParts.Where(t => !existingLotNumbers.Any(existingLotNo => existingLotNo == t.LotNo)).ToList();

        return input;
    }

    #region lot note
    public async Task<MutationResult<Lot>> SetLotNote(LotNoteInput input) {
        MutationResult<Lot> paylaod = new();
        paylaod.Errors = await ValidateSetLotNote(input);
        if (paylaod.Errors.Any()) {
            return paylaod;
        }
        var lot = await context.Lots.FirstAsync(t => t.LotNo == input.LotNo);
        lot.Note = input.Note;

        await context.SaveChangesAsync();
        paylaod.Payload = lot;
        return paylaod;
    }

    public async Task<List<Error>> ValidateSetLotNote(LotNoteInput input) {
        var errors = new List<Error>();
        var lot = await context.Lots.FirstOrDefaultAsync(t => t.LotNo == input.LotNo);

        if (lot == null) {
            errors.Add(new Error("LotNo", $"Lot not found {input.LotNo}"));
        }
        return errors;
    }
    #endregion
}

