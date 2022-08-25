# RMA SKD Middlware Graphql Server

Record and post component sseiral data to Fords "Data Collection Web Service"

## Projects

* skd.server
* skd.model
* skd.dcws
* skd.test
* skd.seed 
* skd.test

## run

```bash
dotnet run --project SKD.Server
```

Suggest using Docker for sql server.

## generate test data

1. create the db `dotnet ef...`
2. start the server
3. generate components and production stations
4. generate production plants
5. import BOM/Lots and shipments

### create the DB / migrate

dotnet ef database update --project skd.server

### start the server

From solution folder

```bash
dotnet run --project skd.server
```

### Create component and stations

```bash
curl http://localhost:5100/gen_ref_data -X POST -d "{}"
```


## Database migration

Install the `dotnet-ef` tooling globally

```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

### Add migrations

```bash
dotnet ef --startup-project skd.server migrations add MigrationName --project skd.model
```

### Remove migratins

```bash
dotnet ef --startup-project skd.server migrations remove --project skd.model
```

### Update database

```bash
dotnet ef database update --project skd.server
dotnet ef database update --project skd.server --connection your_connection_string
```

### Revert to specific migration  (down)

```bash
dotnet ef database update Migration_Name --project skd.server
dotnet ef database update Migration_Name --connection your_connection_string

dotnet ef database update --project skd.server
dotnet ef database update --project skd.server --connection "target connection string"
```

### List migrations

```bash
dotnet ef migrations list --project skd.server
```
