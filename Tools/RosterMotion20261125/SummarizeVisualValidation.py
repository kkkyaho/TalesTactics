import json,hashlib
from pathlib import Path
root=Path(r"C:/dev/TalesTactics");logs=root/"Logs/RosterMotion20261125";out=root/"Docs/RosterMotion20261125/Validation";out.mkdir(exist_ok=True)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def source(p):return {"path":p.relative_to(root).as_posix(),"sha256":sha(p)}
def write(name,data):
    p=out/name;assert not p.exists(),str(p)
    p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
imports=[]
for name in ("first","second"):
    p=logs/"Import"/(name+".json");r=json.loads(p.read_text(encoding="utf-8-sig"));q=r["report"];assert r["passed"] and q["passed"]
    sheets=q["sheets"]
    assert len(sheets)==18 and all(s["capabilitiesPassed"] and s["nativeSourceUnchanged"] for s in sheets)
    imports.append({"source":source(p),"runId":r["runId"],"startedUtc":r["startedUtc"],"finishedUtc":r["finishedUtc"],"passed":True,"characters":q["characters"],"sprites":q["sprites"],"sheets":len(sheets),"capabilitiesPassed":True,"nativePngBytesUnchanged":True,"preservedSpriteIds":sum(s["preservedSpriteIds"] for s in sheets),"preservedLocalFileIds":sum(s["preservedLocalFileIds"] for s in sheets)})
assert imports[1]["preservedSpriteIds"]==imports[1]["preservedLocalFileIds"]==288
write("import-summary.json",{"schemaVersion":1,"scope":"Two actual Unity Sprite Data Provider imports; second run verifies references survive a rerun. Not an EditMode test count.","passed":True,"runs":imports})
boards=[]
for cid in ("cless","mint","farah","tear","jade","natalia","velvet","alphen","shionne","kisara"):
    p=logs/"Captures"/cid/"Actual/result.json";r=json.loads(p.read_text(encoding="utf-8-sig"))
    assert r["complete"] and r["passed"] and r["completedCaptures"]==8 and not r["errors"]
    boards.append({"characterId":cid,"source":source(p),"passed":True,"completedCaptures":8,"startedUtc":r["startedUtc"],"finishedUtc":r["finishedUtc"],"temporaryCampaignUnchanged":r["campaignsUnchanged"],"userTopLevelFilesUnchangedDuringRun":r["userFilesUnchanged"],"rotations":sorted(set(x["rotation"] for x in r["rows"])),"resolutions":sorted(set((x["width"],x["height"]) for x in r["rows"])),"tilePickChecks":sum(x["pickChecks"] for x in r["rows"]),"tilePickMatches":sum(x["pickMatches"] for x in r["rows"]),"issues":sum(len(x["issues"]) for x in r["rows"])})
write("actual-board-summary.json",{"schemaVersion":1,"passed":True,"characters":10,"captures":80,"runs":boards,"scope":"Actual episode-six BoardView renderer/HUD, one selected roster hero with five allies. Temporary all-joined presentation profile, four camera rotations, focus/hit display and two landscape resolutions.","limits":"Not natural recruitment, campaign completion, combat-command execution, performance or Android-device play. Automated pass does not imply all 80 frames were visually reviewed. Whole user tree preservation is checked separately."})
p=logs/"Captures/actual-palette.json";r=json.loads(p.read_text(encoding="utf-8-sig"))
states=[]
for n in ("01","02"):
    p2=logs/"Captures"/("actual-state-"+n+".json.normalized.json");v=json.loads(p2.read_text(encoding="utf-8-sig"))["result"]["result"]
    states.append({"source":source(p2),**{k:v[k] for k in ("utc","frame","seconds","playing","paused","compiling","background","consoleErrors","consoleWarnings")}})
assert states[1]["frame"]>states[0]["frame"] and all(x["playing"] and not x["paused"] and x["consoleErrors"]==0 for x in states)
whiteNoSprite=[x for x in r["hudImages"] if x["active"] and (x["whiteTint"] or x["grayTint"]) and x["sprite"] is None]
stock=[x for x in r["texts"] if x["active"] and x["defaultFont"] and x["defaultFontSize"]]
write("palette-and-live-frames.json",{"schemaVersion":1,"source":source(p),"renderers":r["rendererCount"],"defaultMaterialCount":r["defaultMaterialCount"],"nullShaderCount":r["nullShaderCount"],"errorShaderCount":r["errorShaderCount"],"particleSystems":r["particles"],"activeDefaultFontAndSizeCount":len(stock),"activeWhiteGrayNoSpriteImages":whiteNoSprite,"hudTintInterpretation":"White tint with an authored panel/icon sprite preserves the image's original colors; it is not an unstyled white panel.","liveFrames":states,"scope":"Read-only telemetry of the actual review scene; palette intent and visual quality are reviewed separately."})
print(json.dumps({"actualBoardCaptures":80,"importPasses":2,"liveFrameDelta":states[1]["frame"]-states[0]["frame"],"activeWhiteGrayNoSpriteImages":len(whiteNoSprite),"unstyledStockText":len(stock)}))
