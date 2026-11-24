"""Copy selected, original Unity capture PNGs and write a small offline evidence gallery."""
from pathlib import Path
import hashlib, json, shutil, html
ROOT=Path(__file__).resolve().parents[2]
LOG=ROOT/"Logs/VisualPilot20261124"
OUT=ROOT/"Docs/VisualPilot20261124/Evidence"
def read(rel):
    p=LOG/rel
    d=json.loads(p.read_text(encoding="utf-8-sig"))
    assert d.get("passed") is True and d.get("complete") is True, str(p)
    return p,d
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
bb,b=read("BeforeCapture/Episode06/result.json")
aa,a=read("ActualEpisode06FinalV2/result.json")
mb,bm=read("MotionBeforeFinal/result.json")
ma,am=read("MotionAfterFinal/result.json")
assert bm["pilot"] is False and am["pilot"] is True
assert set(am["closedIdleDirections"])=={"Front","Back","Left","Right"}
assert b["completedCaptures"]==12 and a["completedCaptures"]==14
for key in ["sessionUnchanged","gridUnchanged","campaignUnchanged","randomUnchanged","contentUnchanged","cameraRestored","temporaryObjectsRemoved"]:
    assert bm[key] and am[key], key
assert not OUT.exists(), "Preserve existing evidence; use a new output directory."
OUT.mkdir(parents=True)
records=[]
def copy(label,source,row,name):
    src=source.parent/row["file"]; dest=OUT/name
    assert src.is_file() and src.suffix==".png"
    shutil.copyfile(src,dest)
    assert sha(src)==sha(dest)
    record={"label":label,"file":name,"source":str(src.relative_to(ROOT)).replace("\\","/"),"sha256":sha(src),
            "width":row["width"],"height":row["height"]}
    for key in ["sprite","phase","viewFacing","elapsed","frame","cameraPosition","cameraEuler","cameraSize","orthographicSize","cameraYaw","rotation"]:
        if key in row: record[key]=row[key]
    records.append(record)
    return record
def boardpair(rotation):
    before=next(r for r in b["rows"] if r["phase"]=="idle" and r["rotation"]==rotation)
    after=next(r for r in a["rows"] if r["phase"]=="idle" and r["rotation"]==rotation)
    assert before["cameraPosition"]==after["cameraPosition"]
    assert abs(before["cameraYaw"]-after["cameraYaw"])<.001
    assert abs(before["orthographicSize"]-after["orthographicSize"])<.001
    return [copy("개선 전",bb,before,f"before-board-r{rotation}.png"),
            copy("개선 후",aa,after,f"after-board-r{rotation}.png")]
def frames(d,phase,direction="Front"):
    return [f for f in d["frames"] if (f["phase"]==phase or (phase=="attack" and f["phase"].startswith("attack-"))) and f["viewFacing"]==direction and not f["fullBoard"]]
def nearest(d,phase,direction="Front",elapsed=0):
    return min(frames(d,phase,direction),key=lambda f:abs(f["elapsed"]-elapsed))
def pair(phase,direction="Front",index=None,elapsed=0):
    after=(next(f for f in frames(am,phase,direction) if f["sprite"]==f"alphen-pilot-{phase}-{direction.lower()}-{index}")
           if index is not None else nearest(am,phase,direction,elapsed))
    before=nearest(bm,phase,direction,after["elapsed"])
    assert before["cameraPosition"]==after["cameraPosition"]
    assert before["cameraEuler"]==after["cameraEuler"]
    assert before["cameraSize"]==after["cameraSize"]
    return [copy("개선 전",mb,before,f"before-{phase}-{direction.lower()}.png"),
            copy("개선 후",ma,after,f"after-{phase}-{direction.lower()}.png")]
sections=[
 ("실제 6화 전장 · 동일 카메라",boardpair(0)),
 ("반대 방향 · 동일 카메라",boardpair(180)),
 ("대기 · 실제 확대 화면",pair("idle")),
 ("일반 공격 · 접촉 원화",pair("attack",index=1)),
 ("피격 · 실제 자세",pair("damage",index=0)),
]
blinks=[]
for direction in ["Front","Back","Left","Right"]:
    row=next(f for f in frames(am,"idle",direction) if f["sprite"]==f"alphen-pilot-idle-{direction.lower()}-2")
    blinks.append(copy(direction,ma,row,f"after-blink-{direction.lower()}.png"))
sections.append(("네 방향 눈감음 · 실제 관측 프레임",blinks))
recover=next(f for f in frames(am,"damage") if f["elapsed"]>=.28 and f["sprite"]=="alphen-pilot-idle-front-0")
sections.append(("피격 후 새 대기 그림으로 복귀",[copy("회복 프레임",ma,recover,"after-damage-recovery.png")]))
wide=[r for r in a["rows"] if r["width"]==2340]
sections.append(("가로 모바일 화면 비율 · Windows Editor 검사",
                 [copy(r["phase"],aa,r,f"after-wide-{i}.png") for i,r in enumerate(wide)]))
manifest={"schema":1,"sourceImagesEdited":False,
 "sourceResults":[{"file":str(p.relative_to(ROOT)).replace("\\","/"),"sha256":sha(p)} for p in [bb,aa,mb,ma]],
 "actualBoardBeforeCount":b["completedCaptures"],"actualBoardAfterCount":a["completedCaptures"],
 "motionBeforeCount":bm["pngCount"],"motionAfterCount":am["pngCount"],
 "closedIdleDirections":am["closedIdleDirections"],"images":records,
 "limits":"All PNGs are original Unity captures. Motion uses an isolated actor on the actual episode-six board; full-board captures use the real battle/HUD. Windows Editor capture is not Android device execution. PNG capture cadence is not game performance."}
(OUT/"manifest.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
parts=['<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>알펜·6화 실제 전후 비교</title>',
'<style>body{margin:0;background:#111a26;color:#e8edf5;font:16px/1.6 system-ui,sans-serif}main{max-width:1420px;margin:auto;padding:28px}h1{font-size:28px}h2{font-size:21px;margin-top:38px}p{max-width:1000px;color:#bacbda}a{color:#83d9d0}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}figure{margin:0;background:#1b293b;border-radius:8px;overflow:hidden}img{display:block;width:100%;height:auto}figcaption{padding:10px 14px}small{display:block;color:#b7c2d1;overflow-wrap:anywhere}@media(max-width:700px){.grid{grid-template-columns:1fr}main{padding:16px}}</style><main>',
'<h1>알펜·6화 그래픽 시범 개선</h1><p>실제 Unity 실행 화면을 원본 그대로 보존한 비교입니다. 위 전장 비교는 같은 카메라와 편성입니다. 확대 동작 비교는 개선된 전장 위에서 검사용 알펜 1명의 기존/새 원화만 바꿔 촬영했습니다. 바닥의 전후 차이는 위 전장 비교에서 확인하세요. 카메라 확대 화면에는 HUD가 없으며 실제 전장 화면에서 HP바와 그림자를 별도로 확인했습니다.</p>',
'<p>대기·눈감음·일반 공격·피격은 새 원화, 이동·기술·궁극기는 기존 원화를 사용합니다. 2340×1080 화면은 Windows Editor 검사이며 휴대폰 플레이 결과가 아닙니다. <a href="manifest.json">원본 해시와 촬영 조건</a></p>']
for title,rs in sections:
    parts.append("<h2>"+html.escape(title)+"</h2><div class=grid>")
    for r in rs:
        detail=r.get("sprite","실제 전장")
        if "elapsed" in r: detail+=f" · {r['elapsed']:.3f}초"
        parts.append(f'<figure><a href="{r["file"]}"><img loading="lazy" src="{r["file"]}" alt="{html.escape(r["label"])}"></a><figcaption>{html.escape(r["label"])}<small>{html.escape(detail)}</small></figcaption></figure>')
    parts.append("</div>")
parts.append("</main></html>")
(OUT/"gallery.html").write_text("\n".join(parts),encoding="utf-8")
print(json.dumps({"passed":True,"images":len(records),"output":str(OUT),"pngBytes":sum((OUT/r["file"]).stat().st_size for r in records)},ensure_ascii=False))
