namespace SKD.Test;
public class KitServiceTest : TestBase {

    public KitServiceTest() {
        context = GetAppDbContext();
        Gen_Baseline_Test_Seed_Data();
    }

    [Fact]
    public async Task Can_import_vin() {
        // setup
        var lot = context.Lots.First();
        var plant = context.Plants.First();
        var partnerPlantCode = Gen_PartnerPLantCode();
        var sequence = 1;

        var input = new VinFile {
            PlantCode = plant.Code,
            PartnerPlantCode = partnerPlantCode,
            Sequence = sequence,
            Kits = lot.Kits.Select(t => new VinFile.VinFileKit {
                LotNo = lot.LotNo,
                KitNo = t.KitNo,
                VIN = Gen_VIN()
            }).ToList()
        };

        // test
        var kits = await context.Kits.Where(t => t.Lot.LotNo == lot.LotNo).ToListAsync();
        var lot_kit_count = kits.Count;
        var with_vin_count = kits.Count(t => !String.IsNullOrWhiteSpace(t.VIN));
        Assert.True(0 == with_vin_count);

        var service = new KitService(context, DateTime.Now);
        var result = await service.ImportVIN(input);

        // assert
        var kitVinImport = await context.KitVinImports.FirstOrDefaultAsync(t => t.Plant.Code == input.PlantCode && t.Sequence == input.Sequence);
        Assert.NotNull(kitVinImport);

        var kitvin_count = await context.KitVins.CountAsync(t => t.KitVinImportId == kitVinImport.Id);
        var expected_count = input.Kits.Count;
        Assert.True(expected_count == kitvin_count);

        // partner code
        Assert.Equal(input.PartnerPlantCode, kitVinImport.PartnerPlantCode);

        kits = await context.Kits.Where(t => t.Lot.LotNo == lot.LotNo).ToListAsync();
        with_vin_count = kits.Count(t => t.VIN != "");
        Assert.Equal(lot_kit_count, with_vin_count);
    }

    [Fact]
    public async Task Cannot_re_import_vin_file_with_same_plant_and_sequence() {
        // setup
        var lot = context.Lots.First();
        var plant = context.Plants.First();
        var partnerPlantCode = Gen_PartnerPLantCode();
        var sequence = 1;

        var input = new VinFile {
            PlantCode = plant.Code,
            PartnerPlantCode = partnerPlantCode,
            Sequence = sequence,
            Kits = lot.Kits.Select(t => new VinFile.VinFileKit {
                LotNo = lot.LotNo,
                KitNo = t.KitNo,
                VIN = Gen_VIN()
            }).ToList()
        };

        // test
        var service = new KitService(context, DateTime.Now);
        await service.ImportVIN(input);
        var result = await service.ImportVIN(input);

        // assert
        var expectedErrorMessage = "Already imported VIN file";
        var actualErrorMessage = result.Errors.Select(t => t.Message).FirstOrDefault();
        Assert.StartsWith(expectedErrorMessage, actualErrorMessage);
    }

    [Fact]
    public async Task VIN_Import_can_change_vin() {
        // setup
        var lot = context.Lots.First();
        var plant = context.Plants.First();
        var partnerPlantCode = Gen_PartnerPLantCode();
        var sequence = 1;

        var firstInput = new VinFile {
            PlantCode = plant.Code,
            PartnerPlantCode = partnerPlantCode,
            Sequence = sequence,
            Kits = lot.Kits.Select(t => new VinFile.VinFileKit {
                LotNo = lot.LotNo,
                KitNo = t.KitNo,
                VIN = Gen_VIN()
            }).ToList()
        };

        var newVin = Gen_VIN();
        var change_vin_kitNo = firstInput.Kits.Select(t => t.KitNo).First();

        var secondInput = new VinFile {
            PlantCode = firstInput.PlantCode,
            PartnerPlantCode = firstInput.PartnerPlantCode,
            Sequence = firstInput.Sequence + 1,
            Kits = firstInput.Kits.Select(t => new VinFile.VinFileKit {
                LotNo = lot.LotNo,
                KitNo = t.KitNo,
                VIN = t.KitNo == change_vin_kitNo ? newVin : t.VIN
            }).ToList()
        };

        // first import kit vinds
        var service = new KitService(context, DateTime.Now);
        await service.ImportVIN(firstInput);
        var kitVinCount_before = await context.KitVins.CountAsync();

        // now import with one VIN change
        var paylaod = service.ImportVIN(secondInput);

        var kitVinCount_after = await context.KitVins.CountAsync();
        Assert.Equal(kitVinCount_before + 1, kitVinCount_after);

        // assert vin match for each kit
        foreach (var inputKit in secondInput.Kits.ToList()) {
            var kit = await context.Kits
                .Include(t => t.KitVins)
                .FirstOrDefaultAsync(t => t.KitNo == inputKit.KitNo);
            Assert.Equal(inputKit.VIN, kit.VIN);

            var kitVins_count = kit.KitVins.Count();

            if (inputKit.VIN == newVin) {
                Assert.Equal(2, kitVins_count);
                // assert previous kit-vin entry flagged: RemovedAt
                var old_kit_vin = kit.KitVins.First(t => t.VIN != newVin);
                Assert.NotNull(old_kit_vin.RemovedAt);
            } else {
                Assert.Equal(1, kitVins_count);
            }

        }
    }

    [Fact]
    public async Task Cannot_import_kit_vins_if_kits_not_found() {
        // setup
        var lot = context.Lots.First();
        var plant = context.Plants.First();
        var partnerPlantCode = Gen_PartnerPLantCode();
        var sequence = 3;

        var kitVinDto = new VinFile {
            PlantCode = plant.Code,
            PartnerPlantCode = partnerPlantCode,
            Sequence = sequence,
            Kits = lot.Kits.Select(t => new VinFile.VinFileKit {
                LotNo = lot.LotNo,
                KitNo = Gen_KitNo(), // generate a kit not thats different
                VIN = Gen_VIN()
            }).ToList()
        };

        // test
        var service = new KitService(context, DateTime.Now);
        var result = await service.ImportVIN(kitVinDto);

        // assert
        var expected_error = "kit numbers not found";
        var actual_error = result.Errors.Select(t => t.Message).FirstOrDefault();
        Assert.StartsWith(expected_error, actual_error);
    }

    [Fact]
    public async Task Cannot_import_vins_with_duplicate_KitNo_in_payload() {
        // setup
        var lot = context.Lots.First();
        var plant = context.Plants.First();
        var partnerPlantCode = Gen_PartnerPLantCode();
        var sequence = 3;

        var lotVehicles = lot.Kits.ToList();

        var input = new VinFile {
            PlantCode = plant.Code,
            PartnerPlantCode = partnerPlantCode,
            Sequence = sequence,
        };

        input.Kits = new List<VinFile.VinFileKit>() {
                new VinFile.VinFileKit {LotNo = lot.LotNo, KitNo = lotVehicles[0].KitNo, VIN = Gen_VIN() },
                new VinFile.VinFileKit {LotNo = lot.LotNo, KitNo = lotVehicles[1].KitNo, VIN = Gen_VIN() },
                new VinFile.VinFileKit {LotNo = lot.LotNo, KitNo = lotVehicles[2].KitNo, VIN = Gen_VIN() },
                new VinFile.VinFileKit {LotNo = lot.LotNo, KitNo = lotVehicles[3].KitNo, VIN = Gen_VIN() },
                // duplicate kits
                new VinFile.VinFileKit {LotNo = lot.LotNo, KitNo = lotVehicles[5].KitNo, VIN = Gen_VIN() },
                new VinFile.VinFileKit {LotNo = lot.LotNo, KitNo = lotVehicles[5].KitNo, VIN = Gen_VIN() },
            };
        // test
        var service = new KitService(context, DateTime.Now);
        var result_2 = await service.ImportVIN(input);

        // assert
        var expectedError = "duplicate kitNo(s) in payload";
        var errorMessage = result_2.Errors.Select(t => t.Message).FirstOrDefault();
        errorMessage = errorMessage.Substring(0, expectedError.Length);
        Assert.StartsWith(expectedError, errorMessage);
    }

    [Fact]
    public async Task Can_create_kit_timeline_events() {
        var baseDate = DateTime.Now.Date;
        // setup        
        var dealerCode = await context.Dealers.Select(t => t.Code).FirstOrDefaultAsync();
        var timelineEvents = new List<(TimeLineEventCode eventType, DateTime trxDate, DateTime eventDate)>() {
                (TimeLineEventCode.CUSTOM_RECEIVED, baseDate.AddDays(0), baseDate.AddDays(-6)),
                (TimeLineEventCode.PLAN_BUILD,  baseDate.AddDays(0),baseDate.AddDays(2)),
                (TimeLineEventCode.VERIFY_VIN, baseDate.AddDays(1), baseDate.AddDays(2)),
                (TimeLineEventCode.BUILD_COMPLETED,  baseDate.AddDays(5),baseDate.AddDays(5)),
                (TimeLineEventCode.GATE_RELEASED, baseDate.AddDays(10), baseDate.AddDays(10)),
                (TimeLineEventCode.WHOLE_SALE, baseDate.AddDays(12), baseDate.AddDays(12)),
            };

        // test
        var kit = context.Kits.First();
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        var results = new List<MutationResult<KitTimelineEvent>>();

        var before_count = context.KitTimelineEvents.Count();

        foreach (var entry in timelineEvents) {
            var service = new KitService(context, entry.trxDate);
            var dto = new KitTimelineEventInput {
                KitNo = kit.KitNo,
                EventCode = entry.eventType,
                EventDate = entry.eventDate,
                DealerCode = dealerCode
            };

            if (entry.eventType == TimeLineEventCode.VERIFY_VIN) {
                await Gen_KitVinImport(kit.KitNo, Gen_VIN());
            }
            var result = await service.CreateKitTimelineEvent(dto);
            results.Add(result);
        }

        // assert
        var after_count = context.KitTimelineEvents.Count();
        Assert.Equal(0, before_count);
        Assert.Equal(timelineEvents.Count, after_count);
    }

    [Fact]
    public async Task Error_if_custom_receive_date_greater_than_current_date() {

        // setup
        var appSettings = await ApplicationSetting.GetKnownAppSettings(context);
        var kit = context.Kits.First();
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        var currentDate = DateTime.Now.Date;
        var service = new KitService(context, currentDate);

        var input_1 = new KitTimelineEventInput {
            KitNo = kit.KitNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = currentDate
        };

        var input_2 = new KitTimelineEventInput {
            KitNo = kit.KitNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = currentDate.AddDays(-1)
        };

        // test
        var result_1 = await service.CreateKitTimelineEvent(input_1);
        var result_2 = await service.CreateKitTimelineEvent(input_2);

        // assert
        var expectedError = $"Custom received date must preceed current date by {appSettings.PlanBuildLeadTimeDays} days";
        var actualMessage = result_1.Errors.Select(t => t.Message).FirstOrDefault();
        Assert.StartsWith(expectedError, actualMessage);

        var errorCount = result_2.Errors.Count;
        Assert.Equal(0, errorCount);
    }

    [Fact]
    public async Task Cannot_create_kit_timeline_events_out_of_sequence() {
        // setup
        var baseDate = DateTime.Now.Date;
        var timelineEvents = new List<(TimeLineEventCode eventType, DateTime trxDate, DateTime eventDate)>() {
                (TimeLineEventCode.CUSTOM_RECEIVED, baseDate.AddDays(1),  baseDate.AddDays(6)),
                (TimeLineEventCode.BUILD_COMPLETED, baseDate.AddDays(2), baseDate.AddDays(2)),
            };

        var kit = context.Kits.First();
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        // test
        KitService service = null;
        var results = new List<MutationResult<KitTimelineEvent>>();

        foreach (var (eventType, eventDate, trxDate) in timelineEvents) {
            var input = new KitTimelineEventInput {
                KitNo = kit.KitNo,
                EventCode = eventType,
                EventDate = eventDate,
            };
            service = new KitService(context, trxDate);
            var result = await service.CreateKitTimelineEvent(input);
            results.Add(result);
        }

        var lastPayload = results[1];

        // assert
        var expectedMessage = "Missing timeline event";
        var actualMessage = lastPayload.Errors.Select(t => t.Message).FirstOrDefault();
        Assert.StartsWith(expectedMessage, actualMessage);
    }

    [Fact]
    public async Task Create_kit_timeline_event_with_note() {
        // setup
        var appSettings = await ApplicationSetting.GetKnownAppSettings(context);
        var kit = context.Kits.First();
        var dealerCode = context.Dealers.First().Code;
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        var eventNote = Util.RandomString(15);
        var baseDate = DateTime.Now.Date;
        var timelineEventItems = new List<(TimeLineEventCode eventType, DateTime trxDate, DateTime eventDate, string eventNode)>() {
                (TimeLineEventCode.CUSTOM_RECEIVED, baseDate.AddDays(2), baseDate.AddDays(-appSettings.PlanBuildLeadTimeDays) , eventNote),
                (TimeLineEventCode.PLAN_BUILD, baseDate.AddDays(3), baseDate.AddDays(5), eventNote),
                (TimeLineEventCode.VERIFY_VIN, baseDate.AddDays(4), baseDate.AddDays(5), eventNote),
                (TimeLineEventCode.BUILD_COMPLETED, baseDate.AddDays(8), baseDate.AddDays(8), eventNote),
                (TimeLineEventCode.GATE_RELEASED, baseDate.AddDays(10), baseDate.AddDays(10), eventNote),
                (TimeLineEventCode.WHOLE_SALE, baseDate.AddDays(11), baseDate.AddDays(11), eventNote),
            };

        // test
        KitService service = null;

        var results = new List<MutationResult<KitTimelineEvent>>();

        foreach (var entry in timelineEventItems) {
            var input = new KitTimelineEventInput {
                KitNo = kit.KitNo,
                EventCode = entry.eventType,
                EventDate = entry.eventDate,
                EventNote = entry.eventNode,
                DealerCode = dealerCode
            };
            service = new KitService(context, entry.trxDate);

            if (entry.eventType == TimeLineEventCode.VERIFY_VIN) {
                await Gen_KitVinImport(input.KitNo, Gen_VIN());
            }

            var result = await service.CreateKitTimelineEvent(input);
            results.Add(result);
        }

        // assert
        var timelineEvents = context.KitTimelineEvents.ToList();

        Assert.Equal(timelineEventItems.Count, timelineEvents.Count);

        timelineEvents.ForEach(entry => {
            Assert.Equal(eventNote, entry.EventNote);
        });
    }

    [Fact]
    public async Task Cannot_set_timline_event_date_to_future_date_for_build_complete_onwards() {
        // setup
        var appSettings = await ApplicationSetting.GetKnownAppSettings(context);
        var kit = context.Kits.First();
        var dealerCode = context.Dealers.First().Code;
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        var eventNote = Util.RandomString(15);
        var baseDate = DateTime.Now.Date;
        var timelineEventItems = new List<(TimeLineEventCode eventType, DateTime trxDate, DateTime eventDate, string expectedError)>() {
                (TimeLineEventCode.CUSTOM_RECEIVED, baseDate.AddDays(2), baseDate.AddDays(-appSettings.PlanBuildLeadTimeDays) , ""),
                (TimeLineEventCode.PLAN_BUILD, baseDate.AddDays(3), baseDate.AddDays(5), ""),
                (TimeLineEventCode.VERIFY_VIN, baseDate.AddDays(4), baseDate.AddDays(5), ""),
                (TimeLineEventCode.BUILD_COMPLETED, baseDate.AddDays(8), baseDate.AddDays(9), "Date cannot be in the future"),
            };

        // test
        KitService service = null;

        var results = new List<MutationResult<KitTimelineEvent>>();

        foreach (var entry in timelineEventItems) {
            var input = new KitTimelineEventInput {
                KitNo = kit.KitNo,
                EventCode = entry.eventType,
                EventDate = entry.eventDate,
                EventNote = "",
                DealerCode = dealerCode
            };
            service = new KitService(context, entry.trxDate);

            if (entry.eventType == TimeLineEventCode.VERIFY_VIN) {
                await Gen_KitVinImport(input.KitNo, Gen_VIN());
            }

            var result = await service.CreateKitTimelineEvent(input);
            if (entry.expectedError == "") {
                Assert.True(result.Errors.Count == 0);
            } else {
                Assert.True(result.Errors.Count > 0);
                var actualError = result.Errors.Select(t => t.Message).FirstOrDefault();
                Assert.StartsWith(entry.expectedError, actualError);
            }
            results.Add(result);
        }
    }

    [Fact]
    public async Task Create_kit_timline_event_removes_prior_events_of_the_same_type() {
        // setup
        var kit = context.Kits.First();
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        var before_count = context.KitTimelineEvents.Count();

        var originalDate = new DateTime(2020, 11, 28);
        var newDate = new DateTime(2020, 11, 30);

        var dto = new KitTimelineEventInput {
            KitNo = kit.KitNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = originalDate
        };
        var dto2 = new KitTimelineEventInput {
            KitNo = kit.KitNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = newDate
        };

        var service = new KitService(context, DateTime.Now);
        // test
        await service.CreateKitTimelineEvent(dto);
        await service.CreateKitTimelineEvent(dto2);

        var after_count = context.KitTimelineEvents.Count();

        // assert
        Assert.Equal(0, before_count);
        Assert.Equal(2, after_count);

        var originalEntry = context.KitTimelineEvents.FirstOrDefault(t => t.Kit.VIN == kit.VIN && t.RemovedAt != null);
        var latestEntry = context.KitTimelineEvents.FirstOrDefault(t => t.Kit.VIN == kit.VIN && t.RemovedAt == null);

        Assert.Equal(originalEntry.EventDate, originalDate);
        Assert.Equal(newDate, latestEntry.EventDate);
    }

    [Fact]
    public async Task Cannot_add_duplicate_kit_timline_event_if_same_type_and_date_and_note() {
        // setup
        var kit = context.Kits.First();
        await Gen_ShipmentLot_ForKit(kit.KitNo);

        var originalDate = new DateTime(2020, 11, 28);
        var newDate = new DateTime(2020, 11, 30);
        var eventNote = "EN 78889";

        var dto = new KitTimelineEventInput {
            KitNo = kit.KitNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = originalDate,
            EventNote = eventNote
        };
        var dto2 = new KitTimelineEventInput {
            KitNo = kit.KitNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = newDate,
            EventNote = eventNote
        };

        // test
        var service = new KitService(context, DateTime.Now);
        await service.CreateKitTimelineEvent(dto);
        await service.CreateKitTimelineEvent(dto2);
        var result = await service.CreateKitTimelineEvent(dto2);

        // assert
        var after_count = context.KitTimelineEvents.Count();
        Assert.Equal(2, after_count);
        var errorsMessage = result.Errors.Select(t => t.Message).First();
        var expectedMessage = "duplicate kit timeline event";

        Assert.StartsWith(expectedMessage, errorsMessage);
    }

    [Fact]
    public async Task Can_create_kit_timeline_event_by_lot() {
        // setup
        var lot = context.Lots
            .Include(t => t.Kits)
            .First();
        await Gen_ShipmentLot(lot.LotNo);

        var kitCount = lot.Kits.Count;

        var baseDate = DateTime.Now.Date;
        var eventDate = baseDate.AddDays(-10);
        var eventNote = Util.RandomString(EntityFieldLen.Event_Note);
        var input = new LotTimelineEventInput {
            LotNo = lot.LotNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = eventDate,
            EventNote = eventNote
        };

        // test
        var service = new KitService(context, baseDate);
        var result = await service.CreateLotTimelineEvent(input);

        var errorCount = result.Errors.Count;
        Assert.Equal(0, errorCount);

        var timelineEvents = context.KitTimelineEvents.Where(t => t.Kit.Lot.LotNo == input.LotNo)
            .Include(t => t.Kit)
            .Include(t => t.EventType).ToList();

        var timelineEventCount = timelineEvents.Count;
        Assert.Equal(kitCount, timelineEventCount);

        foreach (var timelineEvent in timelineEvents) {
            Assert.Equal(eventDate, timelineEvent.EventDate);
            Assert.Equal(eventNote, timelineEvent.EventNote);
        }
    }

    [Fact]
    public async Task Cannot_create_kit_timeline_event_by_lot_with_dupliate_date() {
        // setup
        var lot = context.Lots.First();
        await Gen_ShipmentLot(lot.LotNo);

        var baseDate = DateTime.Now.Date;
        var event_date = baseDate.AddDays(1);
        var event_date_trx = baseDate.AddDays(2);
        var eventNote = Util.RandomString(EntityFieldLen.Event_Note);
        var input = new LotTimelineEventInput {
            LotNo = lot.LotNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = event_date,
            EventNote = eventNote
        };

        // test
        var service = new KitService(context, event_date_trx);
        var result = await service.CreateLotTimelineEvent(input);

        var errorCount = result.Errors.Count;
        Assert.Equal(0, errorCount);

        var result_2 = await service.CreateLotTimelineEvent(input);
        var errorCount_2 = result_2.Errors.Count;
        Assert.Equal(1, errorCount_2);

        var errorMessage = result_2.Errors.Select(t => t.Message).FirstOrDefault();
        var expectedMessage = "duplicate kit timeline event";
        var actualMessage = errorMessage.Substring(0, expectedMessage.Length);
        Assert.Equal(expectedMessage, actualMessage);
    }

    [Fact]
    public async Task Cannot_create_lot_custom_receive_with_date_6_months_ago() {
        // setup
        var lot = context.Lots.First();
        await Gen_ShipmentLot(lot.LotNo);

        var baseDate = DateTime.Now.Date;
        var event_date = baseDate.AddMonths(-6).AddDays(-1);
        var eventNote = Util.RandomString(EntityFieldLen.Event_Note);
        var input = new LotTimelineEventInput {
            LotNo = lot.LotNo,
            EventCode = TimeLineEventCode.CUSTOM_RECEIVED,
            EventDate = event_date,
            EventNote = eventNote
        };

        // test
        var service = new KitService(context, baseDate);
        var result = await service.CreateLotTimelineEvent(input);

        var expectedError = "custom received cannot be more than 6 months ago";
        var actualErrorMessage = result.Errors.Select(t => t.Message).FirstOrDefault();
        Assert.Equal(expectedError, actualErrorMessage);
    }

    [Fact]
    public async Task Can_change_kit_component_production_station() {
        // setup
        var kit = await context.Kits
            .Include(t => t.KitComponents).ThenInclude(t => t.ProductionStation)
            .FirstOrDefaultAsync();
        var productionStationCodes = await context.ProductionStations.Select(t => t.Code).ToListAsync();
        var kitComponent = kit.KitComponents.FirstOrDefault();
        var newStationCode = productionStationCodes.First(code => code != kitComponent.ProductionStation.Code);

        var service = new KitService(context, DateTime.Now.Date);

        var paylaod = service.ChangeKitComponentProductionStation(new KitComponentProductionStationInput {
            KitComponentId = kitComponent.Id,
            ProductionStationCode = newStationCode
        });

        var kitComponent_2 = await context.KitComponents
            .Include(t => t.ProductionStation)
            .FirstOrDefaultAsync(t => t.Id == kitComponent.Id);

        Assert.Equal(newStationCode, kitComponent_2.ProductionStation.Code);
    }

    [Fact]
    public async Task Kit_pcv_component_diff_works() {

        // setup
        var service = new PcvService(context);

        var pcv = await context.Pcvs
            .Include(t => t.PcvComponents).ThenInclude(t => t.Component)
            .Include(t => t.PcvComponents).ThenInclude(t => t.ProductionStation)
            .FirstAsync();

        var kit = await context.Kits
            .Include(t => t.KitComponents).ThenInclude(t => t.Component)
            .Include(t => t.KitComponents).ThenInclude(t => t.ProductionStation)
            .Where(t => t.Lot.Pcv.Code == pcv.Code).FirstOrDefaultAsync();

        var diff = await service.GetKitPcvComponentDiff(kit.KitNo);
        Assert.True(0 == diff.InModelButNoKit.Count); // ensure pcv compoents maatch kit componetns
        Assert.True(0 == diff.InKitButNoModel.Count); // ensure pcv compoents maatch kit componetns

        var componentToRemove = pcv.PcvComponents
            .OrderBy(t => t.ProductionStation.Code).ThenBy(t => t.Component.Code)
            .First();
        // remove pcv component 
        var saveModelInput = new PcvInput {
            Id = pcv.Id,
            Code = pcv.Code,
            Description = pcv.Description,
            ComponentStationInputs = pcv.PcvComponents
                .Where(t =>
                    !(
                        t.Component.Code == componentToRemove.Component.Code &&
                        t.ProductionStation.Code == componentToRemove.ProductionStation.Code
                    ))
                .Select(t => new ComponentStationInput {
                    ComponentCode = t.Component.Code,
                    ProductionStationCode = t.ProductionStation.Code
                }).ToList()
        };

        await service.Save(saveModelInput);

        diff = await service.GetKitPcvComponentDiff(kit.KitNo);
        Assert.True(1 == diff.InKitButNoModel.Count);

        Assert.Equal(componentToRemove.Component.Code, diff.InKitButNoModel[0].ComponentCode);
        Assert.Equal(componentToRemove.Component.Code, diff.InKitButNoModel[0].ComponentCode);
    }

    [Fact]
    public async Task Can_sync_kit_pcv_components_if_pcv_component_removed() {
        // setup
        var pcv = await context.Pcvs.FirstOrDefaultAsync();
        await RemoveOneComponentFromModel(pcv);

        var kit = await context.Kits.Where(t => t.Lot.ModelId == pcv.Id).FirstOrDefaultAsync();

        var service = new PcvService(context);
        var diff = await service.GetKitPcvComponentDiff(kit.KitNo);

        Assert.True(1 == diff.InKitButNoModel.Count);

        // test 1: will remove pcv from kit
        await service.SyncKfitPcvComponents(kit.KitNo);

        diff = await service.GetKitPcvComponentDiff(kit.KitNo);
        Assert.True(0 == diff.InKitButNoModel.Count);
        Assert.True(0 == diff.InModelButNoKit.Count);
    }

    [Fact]
    public async Task Can_sync_kit_pcv_components_if_kit_component_removed() {
        // setup
        var kit = await context.Kits
            .Include(t => t.KitComponents)
            .FirstOrDefaultAsync();
        var kitComponet = kit.KitComponents.First();
        kitComponet.RemovedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var service = new PcvService(context);
        var diff = await service.GetKitPcvComponentDiff(kit.KitNo);

        Assert.True(1 == diff.InModelButNoKit.Count);
        Assert.True(0 == diff.InKitButNoModel.Count);

        // test 1: will remove pcv from kit
        await service.SyncKfitPcvComponents(kit.KitNo);

        diff = await service.GetKitPcvComponentDiff(kit.KitNo);
        Assert.True(0 == diff.InKitButNoModel.Count);
        Assert.True(0 == diff.InModelButNoKit.Count);
    }

    private async Task RemoveOneComponentFromModel(PCV pcv) {

        var componentToRemove = pcv.PcvComponents
            .OrderBy(t => t.ProductionStation.Code).ThenBy(t => t.Component.Code)
            .First();

        var saveModelInput = new PcvInput {
            Id = pcv.Id,
            Code = pcv.Code,
            Description = pcv.Description,
            ComponentStationInputs = pcv.PcvComponents
                .Where(t =>
                    !(
                        t.Component.Code == componentToRemove.Component.Code &&
                        t.ProductionStation.Code == componentToRemove.ProductionStation.Code
                    ))
                .Select(t => new ComponentStationInput {
                    ComponentCode = t.Component.Code,
                    ProductionStationCode = t.ProductionStation.Code
                }).ToList()
        };
        var service = new PcvService(context);
        await service.Save(saveModelInput);
    }
}

