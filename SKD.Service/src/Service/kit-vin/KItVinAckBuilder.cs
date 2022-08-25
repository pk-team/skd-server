#nullable enable

namespace SKD.Service;

public class KitVinAckBuilder {

    private readonly SkdContext context;
    public KitVinAckBuilder(SkdContext context) {
        this.context = context;
    }

    public async Task<KitVinAckDTO> GenerateKitVinAcknowledgment(string plantCode, int sequence) {
        KitVinImport? kitVinImport = await context.KitVinImports
            .Include(t => t.Plant)
            .Include(t => t.KitVins).ThenInclude(t => t.Kit)
            .FirstOrDefaultAsync(t => t.Plant.Code == plantCode && t.Sequence == sequence);

        if (kitVinImport == null) {
            return new KitVinAckDTO {
                PlantCode = plantCode,
                Sequence = sequence,
                ErrorMessage = $"KitVinImport not found for {plantCode} - {sequence}",
            };
        }

        var lines = new List<string>();

        lines.Add(BuildHeaderLine(kitVinImport));
        lines.Add(BuildDetailLine(kitVinImport));
        lines.Add(BuildTrailerLine(kitVinImport));

        var dto = new KitVinAckDTO {
            PlantCode = kitVinImport.Plant.Code,
            Sequence = kitVinImport.Sequence,
            Filename = BuildFilename(kitVinImport),
            PayloadText = String.Join('\n', lines)
        };
        return dto;
    }

    public string BuildFilename(KitVinImport kitVinImport) {
        var date = DateTime.UtcNow.ToString("yyyy_MM_dd_hh_mm");
        var sequence = kitVinImport.Sequence.ToString().PadLeft(6, '0');
        return $"{KitVinAckLayout.HDR_FILE_NAME_VAL}_{kitVinImport.Plant.Code}_{sequence}_{date}.txt";
    }

    private string BuildHeaderLine(KitVinImport kitVinImport) {
        var headerLayout = new KitVinAckLayout.Header();
        var lineBuilder = new FlatFileLine<KitVinAckLayout.Header>();

        var fields = new List<FlatFileLine<KitVinAckLayout.Header>.FieldValue>{
                lineBuilder.CreateFieldValue(t => t.HDR_RECORD_TYPE, KitVinAckLayout.HDR_RECORD_TYPE_VAL),
                lineBuilder.CreateFieldValue(t => t.HDR_FILE_NAME, KitVinAckLayout.HDR_FILE_NAME_VAL),
                lineBuilder.CreateFieldValue(t => t.HDR_KD_PLANT_GSDB, kitVinImport.Plant.Code),
                lineBuilder.CreateFieldValue(t => t.HDR_PARTNER_GSDB, kitVinImport.Plant.PartnerPlantCode),
                lineBuilder.CreateFieldValue(t => t.HDR_PARTNER_TYPE, kitVinImport.Plant.PartnerPlantType),
                lineBuilder.CreateFieldValue(t => t.HDR_SEQ_NBR, kitVinImport.Sequence.ToString().PadLeft(lineBuilder.FieldLength(t => t.HDR_SEQ_NBR), '0')),
                lineBuilder.CreateFieldValue(t => t.HDR_BATCH_DATE, DateTime.UtcNow.ToString(KitVinAckLayout.HDR_BATCH_DATE_FORMAT)),
                lineBuilder.CreateFieldValue(t => t.HDR_FILLER, "".PadLeft(lineBuilder.FieldLength(t => t.HDR_FILLER), ' ')),
            };
        return lineBuilder.Build(fields);
    }

    private string BuildDetailLine(KitVinImport kitVinImport) {
        var headerLayout = new KitVinAckLayout.Detail();
        var lineBuilder = new FlatFileLine<KitVinAckLayout.Detail>();

        var fields = new List<FlatFileLine<KitVinAckLayout.Detail>.FieldValue>{
                lineBuilder.CreateFieldValue(t => t.KVM_ACK_RECORD_TYPE, KitVinAckLayout.DTL_KVM_ACK_RECORD_TYPE_VAL),
                lineBuilder.CreateFieldValue(t => t.KVM_ACK_FILE_STATUS, KitVinAckLayout.DTL_KVM_ACK_FILE_STATUS_ACCEPTED),
                lineBuilder.CreateFieldValue(t => t.KVM_ACK_TOTAL_DTL_RECORD, kitVinImport.KitVins.Count.ToString()),
                lineBuilder.CreateFieldValue(t => t.KVM_ACK_TOTAL_DTL_ACCEPTED, kitVinImport.KitVins.Count.ToString()),
                lineBuilder.CreateFieldValue(t => t.KVM_ACK_TOTAL_DTL_REJECTED, "0"),
                lineBuilder.CreateFieldValue(t => t.KVM_FILLER, "".PadLeft(lineBuilder.FieldLength(t => t.KVM_FILLER),' ')),
            };

        return lineBuilder.Build(fields);
    }

    private string BuildTrailerLine(KitVinImport kitVinImport) {
        var headerLayout = new KitVinAckLayout.Trailer();
        var lineBuilder = new FlatFileLine<KitVinAckLayout.Trailer>();

        var fields = new List<FlatFileLine<KitVinAckLayout.Trailer>.FieldValue>{
                lineBuilder.CreateFieldValue(t => t.TLR_RECORD_TYPE, KitVinAckLayout.Trailer.TLR_RECORD_TYPE_VAL),
                lineBuilder.CreateFieldValue(t => t.TLR_FILE_NAME, KitVinAckLayout.Trailer.TLR_FILE_NAME_VAL),
                lineBuilder.CreateFieldValue(t => t.TLR_KD_PLANT_GSDB, kitVinImport.Plant.Code),
                lineBuilder.CreateFieldValue(t => t.TLR_PARTNER_GSDB, kitVinImport.Plant.PartnerPlantCode),
                lineBuilder.CreateFieldValue(t => t.TLR_TOTAL_RECORDS, kitVinImport.KitVins.Count.ToString()),
                lineBuilder.CreateFieldValue(t => t.TLR_FILLER, "".PadLeft(lineBuilder.FieldLength(t => t.TLR_FILLER), '0')),
            };
        return lineBuilder.Build(fields);
    }
}
