namespace SKD.Server;

[ExtendObjectType(typeof(Query))]
public class ProjectionQueries {

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Component> GetComponents(SkdContext context)
        => context.Components.AsQueryable();

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Part> GetParts(
        SkdContext context
    ) => context.Parts;

    [UseProjection]
    public IQueryable<Plant> GetPlants(SkdContext context) =>
             context.Plants;


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Kit> GetKits(
        SkdContext context,
        string plantCode
    ) => context.Kits
        .Where(t => t.Lot.Plant.Code == plantCode)
        .AsQueryable();


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Lot> GetLots(
        SkdContext context,
        string plantCode
    ) => context.Lots.Where(t => t.Plant.Code == plantCode).AsQueryable();

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]

    public IQueryable<PCV> GetPcvs(
        SkdContext context
    ) => context.Pcvs;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]

    public IQueryable<PcvComponent> GetPcvComponents(
        SkdContext context
    ) => context.PcvComponents;


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PcvModel> GetPcvModels(
        SkdContext contenxt
    ) => contenxt.PcvModels;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<PcvSubmodel> GetPcvSubmodels(
        SkdContext context
    ) => context.PcvSubmodels;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<KitComponent> GetKitComponents(
        SkdContext context
    ) => context.KitComponents;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ComponentSerial> GetComponentSerails(
        SkdContext context
    ) => context.ComponentSerials;


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<DcwsResponse> GetDcwsResponses(
        SkdContext context
    ) => context.DCWSResponses;


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ProductionStation> GetProductionStations(SkdContext context) =>
            context.ProductionStations;


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Shipment> GetShipments(
        SkdContext context
    ) => context.Shipments;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ShipmentPart> GetShipmentParts(
        SkdContext context
    ) => context.ShipmentParts;


    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<HandlingUnit> GetHandlingUnits(
        SkdContext context
    ) => context.HandlingUnits;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<KitVinImport> GetVinImports(
        SkdContext context
    ) => context.KitVinImports;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<KitSnapshotRun> GetKitSnapshotRuns(
        SkdContext context
    ) => context.KitSnapshotRuns;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<KitSnapshot> GetKitSnapshots(
        SkdContext context
    ) => context.KitSnapshots;

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]

    public IQueryable<Dealer> GetDealers(SkdContext context)
        => context.Dealers.AsQueryable();

    [UsePaging(MaxPageSize = 10000)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<KitTimelineEvent> GetKitTimelineEvents(SkdContext context)
        => context.KitTimelineEvents.AsQueryable();
    
}

