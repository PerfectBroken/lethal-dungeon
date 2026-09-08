using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
namespace LethalDungeon.CatalogJson;
public static class RoomCatalogJson
{
    static void Object(JsonElement e,params string[] fields){if(e.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Expected object.");var keys=e.EnumerateObject().Select(p=>p.Name).ToArray();if(keys.Distinct().Count()!=keys.Length||keys.Except(fields).Any()||fields.Except(keys).Any())throw new ArgumentException("Unknown, duplicate or missing field.");}
    static string Text(JsonElement e){var s=e.GetString();if(string.IsNullOrWhiteSpace(s))throw new ArgumentException("Empty string.");return s;}
    static int[] Ints(JsonElement e)=>e.EnumerateArray().Select(x=>x.GetInt32()).ToArray();
    static GridPoint Point(JsonElement e){var a=Ints(e);if(a.Length!=3)throw new ArgumentException("Expected three coordinates.");return new GridPoint(a[0],a[1],a[2]);}
    public static ConfiguredRoomCatalog Parse(string json)
    {
        if(json==null||Encoding.UTF8.GetByteCount(json)>1048576)throw new ArgumentException("JSON exceeds limit.");
        try {
            using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=32});var root=document.RootElement;
            Object(root,"schemaVersion","catalogVersion","metresPerUnit","coordinateSystem","grid","socketDefaults","rooms");
            if(Text(root.GetProperty("schemaVersion"))!="room-catalog-v1"||root.GetProperty("metresPerUnit").GetDecimal()!=0.5m)throw new ArgumentException("Unsupported schema or units.");
            var coordinate=root.GetProperty("coordinateSystem");Object(coordinate,"up","north","east","rotation");
            if(Text(coordinate.GetProperty("up"))!="Y"||Text(coordinate.GetProperty("north"))!="+Z"||Text(coordinate.GetProperty("east"))!="+X"||Text(coordinate.GetProperty("rotation"))!="clockwise viewed from above; (x,y,z) -> (z,y,-x) at 90 degrees")throw new ArgumentException("Unsupported coordinates.");
            var grid=root.GetProperty("grid");Object(grid,"horizontalCellUnits","floorHeightUnits");if(grid.GetProperty("horizontalCellUnits").GetInt32()!=16||grid.GetProperty("floorHeightUnits").GetInt32()!=8)throw new ArgumentException("Unsupported lattice.");
            var defaults=root.GetProperty("socketDefaults");Object(defaults,"clearanceDepthUnits","unusedSocket");if(defaults.GetProperty("clearanceDepthUnits").GetInt32()!=2||Text(defaults.GetProperty("unusedSocket"))!="sealed")throw new ArgumentException("Unsupported socket defaults.");
            var input=root.GetProperty("rooms").EnumerateArray().ToArray();if(input.Length<1||input.Length>128)throw new ArgumentException("Module count.");var patterns=new List<RoomPattern>();
            foreach(var r in input){
                Object(r,"id","name","revision","prefabKey","kind","allowedRotations","occupancy","sockets","traversalGroups","connectionPolicy","matching");
                if(!new[]{"room","stairs","ramp","corridor"}.Contains(Text(r.GetProperty("kind"))))throw new ArgumentException("Unknown kind.");
                var rotations=Ints(r.GetProperty("allowedRotations"));if(rotations.Length==0||rotations.Distinct().Count()!=rotations.Length||rotations.Any(v=>v<0||v>270||v%90!=0))throw new ArgumentException("Invalid rotations.");
                int revision=r.GetProperty("revision").GetInt32();if(revision<1)throw new ArgumentException("Invalid revision.");
                var boxes=r.GetProperty("occupancy").EnumerateArray().Select(b=>{Object(b,"min","max");return new GridBox(Point(b.GetProperty("min")),Point(b.GetProperty("max")));}).ToArray();
                var sockets=r.GetProperty("sockets").EnumerateArray().Select(s=>{Object(s,"id","position","facing","size","connectionType");var f=Text(s.GetProperty("facing"));if(!new[]{"north","east","south","west"}.Contains(f))throw new ArgumentException("Facing.");var size=Ints(s.GetProperty("size"));if(size.Length!=2||size[0]!=4||size[1]!=6||Text(s.GetProperty("connectionType"))!="standard")throw new ArgumentException("Unsupported aperture.");return new DoorSocket(Text(s.GetProperty("id")),Point(s.GetProperty("position")),Enum.Parse<Direction>(f,true),size[0],size[1]);}).ToArray();
                if(sockets.Length<1||sockets.Length>4)throw new ArgumentException("Socket count.");var ids=sockets.Select(s=>s.Id).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
                var policy=r.GetProperty("connectionPolicy");Object(policy,"requiredSockets");var required=policy.GetProperty("requiredSockets").EnumerateArray().Select(Text).OrderBy(s=>s,StringComparer.Ordinal).ToArray();if(!required.SequenceEqual(ids))throw new ArgumentException("All sockets must be required.");
                var groups=r.GetProperty("traversalGroups").EnumerateArray().ToArray();if(groups.Length!=1)throw new ArgumentException("Exactly one traversal group.");Object(groups[0],"id","sockets");Text(groups[0].GetProperty("id"));if(!groups[0].GetProperty("sockets").EnumerateArray().Select(Text).OrderBy(s=>s,StringComparer.Ordinal).SequenceEqual(ids))throw new ArgumentException("Disconnected sockets.");
                var matching=r.GetProperty("matching");Object(matching,"cells","edges","socketCells");var cells=matching.GetProperty("cells").EnumerateArray().Select(Point).ToArray();
                var edges=matching.GetProperty("edges").EnumerateArray().Select(e=>{var a=Ints(e);if(a.Length!=2)throw new ArgumentException("Edge pair.");return (A:a[0],B:a[1]);}).ToArray();
                var socketCells=matching.GetProperty("socketCells");Object(socketCells,ids);
                var definition=new RoomDefinition(Text(r.GetProperty("id")),Text(r.GetProperty("name")),boxes,sockets);
                patterns.Add(new RoomPattern(definition,Text(r.GetProperty("prefabKey")),revision,rotations.Select(v=>v/90),cells,edges,socketCells.EnumerateObject().ToDictionary(p=>p.Name,p=>p.Value.GetInt32())));
            }
            return new ConfiguredRoomCatalog(Text(root.GetProperty("catalogVersion")),patterns);
        } catch(Exception e) when(e is JsonException||e is InvalidOperationException||e is FormatException||e is OverflowException||e is KeyNotFoundException){throw new ArgumentException("Invalid room catalog JSON.",e);}
    }
}
