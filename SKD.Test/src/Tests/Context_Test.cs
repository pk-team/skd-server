namespace SKD.Test;
public class ContextTest : TestBase {

    [Fact]
    public void Can_add_component() {
        using var ctx = GetAppDbContext();
        // setup
        var component = new Component() {
            Code = new String('X', EntityFieldLen.Component_Code),
            Name = new String('X', EntityFieldLen.Component_Name)
        };

        ctx.Components.Add(component);

        // test
        ctx.SaveChanges();

        // assert
        var count = ctx.Components.Count();
        Assert.Equal(1, count);

    }

    [Fact]
    public void Cannot_add_duplication_component_code() {
        using var ctx = GetAppDbContext();
        // setup
        var component_1 = new Component() {
            Code = "Same_Code",
            Name = "Name1",
        };

        var component_2 = new Component() {
            Code = "Same_Code",
            Name = "Name1",
        };

        ctx.Components.Add(component_1);
        ctx.Components.Add(component_2);

        // test + assert
        Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
    }

    [Fact]
    public void Cannot_add_duplication_component_name() {
        var componentName = "SameName";

        using var ctx = GetAppDbContext();
        // setup
        var component_1 = new Component() {
            Code = "Code1",
            Name = componentName,
        };

        var component_2 = new Component() {
            Code = "Code2",
            Name = componentName,
        };

        ctx.Components.Add(component_1);
        ctx.Components.Add(component_2);

        // test + assert
        Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
    }

    [Fact]
    public void Can_add_vehicle_model() {
        using var ctx = GetAppDbContext();
        // setup
        var pcv = new PCV() {
            Code = new String('X', EntityFieldLen.Pcv_Code),
            Description = new String('X', EntityFieldLen.Pcv_Description),
            ModelYear = new String('X', EntityFieldLen.Pcv_Meta),
        };

        ctx.Pcvs.Add(pcv);
        // test
        ctx.SaveChanges();

        // assert
        var count = ctx.Pcvs.Count();
        Assert.Equal(1, count);
    }

    [Fact]
    public void Submit_model_input_twice_has_no_side_effect() {
        using var ctx = GetAppDbContext();
        // setup
        var modelCode = new String('A', EntityFieldLen.Pcv_Code);
        var pcv_1 = new PCV() {
            Code = modelCode,
            Description = new String('A', EntityFieldLen.Pcv_Description),
            ModelYear = new String('A', EntityFieldLen.Pcv_Meta),
        };

        var pcv_2 = new PCV() {
            Code = modelCode,
            Description = new String('B', EntityFieldLen.Pcv_Description),
            ModelYear = new String('B', EntityFieldLen.Pcv_Meta),
        };

        ctx.Pcvs.AddRange(pcv_1, pcv_2);

        // test + assert
        Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());

    }

    [Fact]
    public void Can_add_duplicate_vehicle_model_name() {
        using var ctx = GetAppDbContext();
        // setup
        var modelName = new String('A', EntityFieldLen.Component_Name);
        var pcv_1 = new PCV() {
            Code = new String('A', EntityFieldLen.Pcv_Code),
            Description = modelName,
            ModelYear = new String('A', EntityFieldLen.Pcv_Meta),
        };

        var pcv_2 = new PCV() {
            Code = new String('B', EntityFieldLen.Pcv_Code),
            Description = modelName,
            ModelYear = new String('B', EntityFieldLen.Pcv_Meta),
        };

        ctx.Pcvs.AddRange(pcv_1, pcv_2);
        ctx.SaveChanges();

        var count = ctx.Pcvs.Count(t => t.Description == modelName);
        Assert.Equal(2, count);
    }

    [Fact]
    public void Can_add_kit() {
        using var ctx = GetAppDbContext();
        // setup
        var pcv = new PCV() {
            Code = new String('X', EntityFieldLen.Pcv_Code),
            Description = new String('X', EntityFieldLen.Pcv_Description),
            ModelYear = new String('X', EntityFieldLen.Pcv_Meta),
        };
        ctx.Pcvs.Add(pcv);
        ctx.SaveChanges();

        // plant
        var plant = new Plant {
            Code = Gen_PlantCode(),
            PartnerPlantCode = Gen_PartnerPLantCode(),
            PartnerPlantType = Gen_PartnerPlantType()
        };
        ctx.Plants.Add(plant);

        // bom
        var bom = new Bom { Sequence = 1, Plant = plant };
        ctx.Boms.Add(bom);

        // lot
        pcv = ctx.Pcvs.First();
        var lotNo = new String('X', EntityFieldLen.LotNo);
        var lot = new Lot {
            LotNo = Gen_LotNo(pcv.Code, 1),
            Pcv = pcv,
            Bom = bom,
            Plant = plant
        };
        ctx.Lots.Add(lot);

        // kit 
        var kit = new Kit() {
            VIN = new String('X', EntityFieldLen.VIN),
            Lot = lot,
        };

        ctx.Kits.Add(kit);

        // test
        ctx.SaveChanges();

        // assert
        var kitCount = ctx.Pcvs.Count();
        Assert.Equal(1, kitCount);
    }

    [Fact]
    public void Cannot_add_vehicle_without_model() {
        using var ctx = GetAppDbContext();
        // setup
        var vehicle = new Kit() {
            VIN = new String('X', EntityFieldLen.VIN),
        };

        ctx.Kits.Add(vehicle);

        // test + assert
        Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
    }

    [Fact]
    public void Cannot_add_vehicle_duplicate_vin() {
        using var ctx = GetAppDbContext();
        // setup
        var pcv = new PCV() {
            Code = new String('X', EntityFieldLen.Pcv_Code),
            Description = new String('X', EntityFieldLen.Pcv_Description),
            ModelYear = new String('X', EntityFieldLen.Pcv_Meta),
        };

        ctx.Pcvs.Add(pcv);

        var vehicle_1 = new Kit() {
            VIN = new String('X', EntityFieldLen.VIN),
        };

        var vehicle_2 = new Kit() {
            VIN = new String('X', EntityFieldLen.VIN),
        };

        ctx.Kits.AddRange(vehicle_1, vehicle_2);

        // test + assert
        Assert.Throws<DbUpdateException>(() => ctx.SaveChanges());
    }

    [Fact]
    public void Can_add_parts() {
        using var ctx = GetAppDbContext();
        var parts = new List<Part> {
                    new Part { PartNo = "p1", OriginalPartNo = "p1 -", PartDesc = "p1 desc"},
                    new Part { PartNo = "p2", OriginalPartNo = "p2 -", PartDesc = "p2 desc"},
                };

        ctx.Parts.AddRange(parts);
        var before_count = ctx.Parts.Count();
        Assert.Equal(0, before_count);

        ctx.SaveChanges();

        var after_count = ctx.Parts.Count();
        Assert.Equal(parts.Count, after_count);
    }

    [Fact]
    public void Can_add_dealers() {
        using var ctx = GetAppDbContext();
        var dealers = new List<Dealer> {
                    new Dealer { Code = "D1", Name = "name 1"},
                    new Dealer { Code = "D2", Name = "Name 2"},
                };

        ctx.Dealers.AddRange(dealers);
        var before_count = ctx.Dealers.Count();
        Assert.Equal(0, before_count);

        ctx.SaveChanges();

        var after_count = ctx.Dealers.Count();
        Assert.Equal(dealers.Count, after_count);
    }

    [Fact]
    public void Can_add_PcvModel_Submodel_SubmodelComponents() {
        using var ctx = GetAppDbContext();
        var components = new List<Component>() {
            new Component { Code= "DA", Name = "Driver Airbag"},
            new Component { Code= "PA", Name = "Passenger Airbag"},
        };

        ctx.Components.AddRange(components);
        ctx.SaveChanges();
        //
        var pcvModel = new PcvModel {
            Code = "Ranger",
            Name = "Ranger",
            Submodels = new List<PcvSubmodel> {
                new PcvSubmodel {
                    Code = "P703",
                    Name = "P703",                    
                },
                new PcvSubmodel {
                    Code = "P702",
                    Name = "P702",
                }
            }
        };

        pcvModel.Submodels.ToList().ForEach(subModel => {
            components.ForEach(component => {
                subModel.SubmodelComponents.Add(new PcvSubmodelComponent {
                    Component = component,
                    Submodel = subModel
                });
            });
        });

        ctx.PcvModels.Add(pcvModel);
        ctx.SaveChanges();

        // add pcv
        ctx.Pcvs.Add(new PCV {
            Code = "BPA0A11",
            Body = "DBL CAB",
            Series = "WILDTRAK",
            ModelYear = "2024",
            Description = "Description",
            SubModel = ctx.PcvSubmodels.First(t => t.Code == "P703")
        });
        ctx.SaveChanges();



        // assert
        var models = ctx.PcvModels
            .Include(t => t.Submodels)
            .ThenInclude(t => t.Pcvs)
            .ToList();

        var modelCount = models.Count();
        var subModelCount = models.SelectMany(t => t.Submodels).Count();
        var subModelComponentCount = models.SelectMany(t => t.Submodels).SelectMany(t => t.SubmodelComponents).Count();

        var pcv = models
            .SelectMany(t => t.Submodels)
            .Where(t => t.Pcvs.Any())
            .Select(t => t.Pcvs)
            .First().First();
                 
        Assert.Equal(1, modelCount);
        Assert.Equal(2, subModelCount);
        Assert.Equal(4, subModelComponentCount);       
        Assert.Equal("BPA0A11", pcv.Code); 
    }
}
