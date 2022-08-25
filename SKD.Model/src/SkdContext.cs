#nullable enable

namespace SKD.Model;

public class SkdContext : DbContext {
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<Kit> Kits => Set<Kit>();
    public DbSet<KitVinImport> KitVinImports => Set<KitVinImport>();
    public DbSet<KitVin> KitVins => Set<KitVin>();
    public DbSet<KitTimelineEvent> KitTimelineEvents => Set<KitTimelineEvent>();
    public DbSet<KitTimelineEventType> KitTimelineEventTypes => Set<KitTimelineEventType>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Component> Components => Set<Component>();

    public DbSet<PCV> Pcvs => Set<PCV>();
    public DbSet<PcvModel> PcvModels => Set<PcvModel>();
    public DbSet<PcvSubmodel> PcvSubmodels => Set<PcvSubmodel>();
    public DbSet<PcvSubmodelComponent> PcvSubmodelComponents => Set<PcvSubmodelComponent>();

    public DbSet<KitComponent> KitComponents => Set<KitComponent>();
    public DbSet<ComponentSerial> ComponentSerials => Set<ComponentSerial>();
    public DbSet<DcwsResponse> DCWSResponses => Set<DcwsResponse>();
    public DbSet<PcvComponent> PcvComponents => Set<PcvComponent>();
    public DbSet<ProductionStation> ProductionStations => Set<ProductionStation>();
    public DbSet<Part> Parts => Set<Part>();

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentLot> ShipmentLots => Set<ShipmentLot>();
    public DbSet<ShipmentInvoice> ShipmentInvoices => Set<ShipmentInvoice>();
    public DbSet<HandlingUnit> HandlingUnits => Set<HandlingUnit>();
    public DbSet<HandlingUnitReceived> HandlingUnitReceived => Set<HandlingUnitReceived>();
    public DbSet<ShipmentPart> ShipmentParts => Set<ShipmentPart>();

    public DbSet<Bom> Boms => Set<Bom>();
    public DbSet<LotPart> LotParts => Set<LotPart>();
    public DbSet<LotPartReceived> LotPartsReceived => Set<LotPartReceived>();

    public DbSet<KitSnapshotRun> KitSnapshotRuns => Set<KitSnapshotRun>();
    public DbSet<KitSnapshot> KitSnapshots => Set<KitSnapshot>();
    public DbSet<PartnerStatusAck> PartnerStatusAcks => Set<PartnerStatusAck>();


    public SkdContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder) {
        builder.ApplyConfiguration(new AppSetting_Config());
        
        builder.ApplyConfiguration(new Component_Config());
        builder.ApplyConfiguration(new User_Config());
        builder.ApplyConfiguration(new Plant_Config());
        builder.ApplyConfiguration(new Dealer_Config());

        builder.ApplyConfiguration(new Bom_Config());

        builder.ApplyConfiguration(new Kit_Config());
        builder.ApplyConfiguration(new KitVinImport_Config());
        builder.ApplyConfiguration(new KitVin_Config());
        builder.ApplyConfiguration(new KitSnapshot_Config());
        builder.ApplyConfiguration(new KitSnapshotRun_Config());
        builder.ApplyConfiguration(new KitSnapshotRunAck_Config());
        builder.ApplyConfiguration(new KitTimelineEventType_Config());
        builder.ApplyConfiguration(new KitTimelineEvent_Config());

        builder.ApplyConfiguration(new Lot_Config());
        builder.ApplyConfiguration(new LotPart_Config());
        builder.ApplyConfiguration(new LotPartReceived_Config());

        builder.ApplyConfiguration(new Pcv_Config());
        builder.ApplyConfiguration(new PcvModel_Config());
        builder.ApplyConfiguration(new PcvSubmodel_Config());
        builder.ApplyConfiguration(new PcvSubmodelComponent_Config());

        builder.ApplyConfiguration(new KitComponent_Config());
        builder.ApplyConfiguration(new PcvComponent_Config());
        builder.ApplyConfiguration(new ComponentSerial_Config());
        builder.ApplyConfiguration(new DCWSResponse_Config());
        builder.ApplyConfiguration(new ProductionStation_Config());
        builder.ApplyConfiguration(new Part_Config());
        //
        builder.ApplyConfiguration(new Shipment_Config());
        builder.ApplyConfiguration(new ShipmentLot_Config());
        builder.ApplyConfiguration(new ShipmentInvoice_Config());
        builder.ApplyConfiguration(new HandlingUnit_Config());
        builder.ApplyConfiguration(new HandlingUnitReceived_Config());
        builder.ApplyConfiguration(new ShipmentPart_Config());
        //

    }
}
