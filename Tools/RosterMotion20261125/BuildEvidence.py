"""Publish unedited actual Unity captures to an offline Korean evidence gallery.

Selection JSON (paths relative to the project, never guessed from folder names):
{"characters":[{"characterId":"cless","before":"Logs/RosterMotion20261125/.../result.json",
"after":"Logs/RosterMotion20261125/.../result.json","board":"Logs/RosterMotion20261125/.../result.json"}]}
Repeat for the ten heroes. --check-only validates without writing. --preview allows
a smaller, explicitly labelled subset; --output must remain under task Evidence.
Publication contains six snapshots per hero and six representative boards only.
Selected snapshots are NEVER played as a continuous animation. Optional --local-output
under this task's Logs produces full observed-sequence playback referencing original
PNGs without copying them. --local-only writes only that local review gallery.
Optional selection boards: [{"characterId":"cless","file":"00-...png"}, ...].
Without an explicit board list, six boards are chosen to cover the roster plus
camera rotation and the 2340x1080 command UI. --check-only reports exact byte totals.
No Unity, Git, network, image encoding, interpolation, or source modification.
"""
from __future__ import annotations
import argparse
import hashlib
import html
import json
import math
import os
import copy
from pathlib import Path, PurePosixPath
import re
import shutil
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
LOG = ROOT / "Logs/RosterMotion20261125"
EVIDENCE = ROOT / "Docs/RosterMotion20261125/Evidence"
HEROES = {"cless":"크레스", "mint":"민트", "farah":"파라", "tear":"티아", "jade":"제이드",
          "natalia":"나탈리아", "velvet":"벨벳", "alphen":"알펜", "shionne":"시온", "kisara":"키사라"}
DIRECTIONS = {"Front":"정면", "Back":"후면", "Left":"왼쪽", "Right":"오른쪽"}
FLAGS = ("sessionUnchanged", "gridUnchanged", "campaignUnchanged", "randomUnchanged",
         "contentUnchanged", "cameraRestored", "temporaryObjectsRemoved")
FRAME_FIELDS = ("file", "phase", "viewFacing", "worldFacing", "sprite", "texture", "frame", "width", "height",
                "realtime", "time", "elapsed", "pixelsPerUnit", "rect", "pivot", "rootPosition", "localPosition",
                "worldPosition", "cameraPosition", "cameraEuler", "cameraSize", "presenting", "fullBoard", "blinkSupplement")
BOARD_FIELDS = ("scenario", "phase", "episode", "stage", "width", "height", "rotation", "cameraYaw",
                "cameraPosition", "orthographicSize", "battlefieldViewport", "allies", "models", "tiles",
                "pickChecks", "pickMatches", "terrainMeshes", "terrainVertices")

def require(ok, message):
    if not ok:
        raise ValueError(message)

def sha(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()

def inside(path, root):
    try:
        path.resolve().relative_to(root.resolve())
        return True
    except ValueError:
        return False

def relative(path):
    return path.resolve().relative_to(ROOT).as_posix()

def read_result(value):
    require(isinstance(value, str) and value, "결과 JSON 경로가 필요합니다.")
    path = (ROOT / value).resolve()
    require(inside(path, LOG) and path.name == "result.json", "이번 작업 Logs 아래의 명시적인 result.json만 허용합니다.")
    require(path.is_file(), f"캡처 결과가 아직 없습니다: {relative(path)}")
    raw = path.read_bytes()
    data = json.loads(raw.decode("utf-8-sig"))
    require(data.get("passed") is True and data.get("complete") is True and not data.get("errors"),
            f"완료·통과한 결과만 공개할 수 있습니다: {relative(path)}")
    return {"path":path, "data":data, "sha256":hashlib.sha256(raw).hexdigest()}

def finite(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)

def vector(value):
    return isinstance(value, dict) and all(finite(value.get(k)) for k in ("x", "y", "z"))

def image_path(result, row):
    name = row.get("file")
    require(isinstance(name, str) and "\\" not in name, "PNG 경로는 상대 POSIX 경로여야 합니다.")
    rel = PurePosixPath(name)
    require(not rel.is_absolute() and ".." not in rel.parts and ":" not in name and rel.suffix == ".png", "잘못된 PNG 경로입니다.")
    path = (result["path"].parent / name).resolve()
    require(inside(path, result["path"].parent) and path.is_file(), f"실제 PNG 누락: {relative(path)}")
    with path.open("rb") as stream:
        header = stream.read(24)
    require(len(header) == 24 and header[:8] == b"\x89PNG\r\n\x1a\n" and header[12:16] == b"IHDR", "PNG 헤더가 아닙니다.")
    width, height = struct.unpack(">II", header[16:24])
    require(width > 0 and height > 0 and (width, height) == (row.get("width"), row.get("height")), "관측값과 PNG 크기가 다릅니다.")
    return path

def authored(cid, kind, direction, index):
    return f"{cid}-{'pilot' if cid == 'alphen' else 'roster'}-{kind}-{direction.lower()}-{index}"

def sequence(result, direction, phase):
    return [f for f in result["data"]["frames"] if not f["fullBoard"] and not f["blinkSupplement"]
            and f["viewFacing"] == direction and (f["phase"] == "idle" if phase == "idle" else f["phase"].startswith(phase))]

def validate_motion(result, cid, after):
    data = result["data"]
    require(data.get("schema") == 1 and data.get("characterId") == cid and data.get("pilot") is after,
            f"캐릭터 또는 전후 구분이 다릅니다: {cid}")
    require(data.get("campaignStage") == 11 and all(data.get(k) is True for k in FLAGS), f"모션 상태 보존 검사 실패: {cid}")
    require(finite(data.get("timeScale")) and data["timeScale"] > 0, "재생 중 관측 결과가 필요합니다.")
    rows = data.get("frames")
    require(isinstance(rows, list) and rows and data.get("pngCount") == len(rows), "모션 PNG 개수가 맞지 않습니다.")
    seen = set()
    for row in rows:
        require(all(k in row for k in FRAME_FIELDS), "프레임 관측 필드가 누락됐습니다.")
        require(row["file"] not in seen, "같은 PNG가 두 번 연결되었습니다.")
        seen.add(row["file"])
        require(row["viewFacing"] in DIRECTIONS and row["phase"] in
                ("idle", "attack-prepare", "attack-release", "attack-recovery", "damage", "damage-recovery"), "지원하지 않는 관측 단계입니다.")
        require(isinstance(row["sprite"], str) and row["sprite"].startswith((cid + "-", cid + "_")), f"다른 캐릭터 원화가 연결되었습니다: {cid}")
        require(isinstance(row["texture"], str) and row["texture"].startswith("Assets/") and ":" not in row["texture"], "상대 에셋 경로가 필요합니다.")
        if not after:
            legacy_textures = {f"Assets/TalesTactics/Art/PixelCampaign/{cid}-hq.png",
                               f"Assets/TalesTactics/Art/PixelCampaign/{cid}-motion.png",
                               f"Assets/TalesTactics/Art/MotionPremium/{cid}.png"}
            if cid == "shionne": legacy_textures.add("Assets/TalesTactics/Art/PixelCampaign/shionne-right.png")
            existing_alphen = cid == "alphen" and row["texture"].startswith("Assets/TalesTactics/Art/VisualPilot/Alphen/")
            require(row["texture"] in legacy_textures or existing_alphen, "기존 프레임의 원화 경로가 해당 동료와 다릅니다.")
        if after and not row["fullBoard"]:
            art_root = "Assets/TalesTactics/Art/VisualPilot/Alphen/" if cid == "alphen" else f"Assets/TalesTactics/Art/RosterMotion20261125/{cid}/"
            require(row["texture"].startswith(art_root), "개선 후 프레임이 해당 동료의 새 원화 경로를 가리키지 않습니다.")
            kind = "idle" if row["phase"] in ("idle", "attack-recovery", "damage-recovery") else "attack" if row["phase"].startswith("attack") else "damage"
            count = {"idle":4, "attack":3, "damage":1}[kind]
            require(row["sprite"] in {authored(cid, kind, row["viewFacing"], i) for i in range(count)},
                    "관측 동작/방향과 실제 새 스프라이트 이름이 다릅니다.")
        require(all(finite(row[k]) for k in ("frame", "realtime", "time", "elapsed", "pixelsPerUnit", "cameraSize"))
                and row["elapsed"] >= 0 and row["pixelsPerUnit"] > 0 and row["cameraSize"] > 0, "잘못된 프레임 시간/크기입니다.")
        require(all(vector(row[k]) for k in ("rootPosition", "localPosition", "worldPosition", "cameraPosition", "cameraEuler")), "카메라/배치 좌표가 유효하지 않습니다.")
        require(all(type(row[k]) is bool for k in ("fullBoard", "blinkSupplement", "presenting")), "프레임 상태 필드가 유효하지 않습니다.")
        image_path(result, row)
    for direction in DIRECTIONS:
        for phase in ("idle", "attack", "damage"):
            frames = sequence(result, direction, phase)
            require(frames, f"실제 시퀀스 누락: {cid}/{direction}/{phase}")
            require(all(b["frame"] > a["frame"] and b["elapsed"] >= a["elapsed"] and b["realtime"] > a["realtime"]
                        and b["time"] >= a["time"] for a, b in zip(frames, frames[1:])), "관측 순서가 실제 시간/프레임 순서와 다릅니다.")
        if after:
            observed = [f for f in rows if f["viewFacing"] == direction and not f["fullBoard"]]
            for kind, count in (("idle", 4), ("attack", 3), ("damage", 1)):
                # Not every idle breathing frame is guaranteed at a sparse PNG cadence.
                indices = (2,) if kind == "idle" else range(count)
                for index in indices:
                    require(any(f["sprite"] == authored(cid, kind, direction, index) for f in observed),
                            f"필수 실제 원화 관측 누락: {cid}/{kind}/{direction}/{index}")
            require(any(f["phase"] == "damage-recovery" and f["sprite"] == authored(cid, "idle", direction, 0) for f in observed),
                    "피격 후 실제 대기 복귀 관측이 없습니다.")
    if after:
        require(set(data.get("closedIdleDirections", [])) == set(DIRECTIONS), "실제 네 방향 눈감음 관측이 필요합니다.")

def validate_pair(before, after):
    for direction in DIRECTIONS:
        a = sequence(before, direction, "idle")[0]
        b = sequence(after, direction, "idle")[0]
        require(all(a[k] == b[k] for k in ("cameraPosition", "cameraEuler", "rootPosition", "width", "height"))
                and abs(a["cameraSize"] - b["cameraSize"]) < .0001, "전후 비교 카메라 또는 배치가 다릅니다.")

def validate_board(result, cid):
    data = result["data"]
    require(data.get("schema") == 2 and data.get("characterId") == cid and data.get("userFilesUnchanged") is True
            and data.get("campaignsUnchanged") is True, "전장 캐릭터 또는 보존 검증이 다릅니다.")
    rows = data.get("rows")
    require(isinstance(rows, list) and len(rows) == data.get("completedCaptures") == data.get("expectedCaptures") == 8,
            "실제 전장 8개 캡처가 완료되지 않았습니다.")
    require(len({row["file"] for row in rows}) == len(rows), "전장 PNG가 중복 연결되었습니다.")
    for row in rows:
        require(row.get("stage") == 11 and row.get("episode") == 6 and not row.get("issues"), "6화 전장 검사가 통과하지 않았습니다.")
        actor = [u for u in row.get("units", []) if u.get("id") == cid]
        require(len(actor) == 1 and actor[0].get("sprite", "").startswith(cid + "-"), "전장에 해당 캐릭터가 정확히 한 명 있어야 합니다.")
        require(all(k in row for k in BOARD_FIELDS) and vector(row["cameraPosition"]), "전장 관측 필드가 누락됐습니다.")
        image_path(result, row)

def select_boards(boards, explicit, ids):
    """Choose at most five roster-covering views and one wide view; never infer identity from filenames."""
    chosen, keys, covered = [], set(), set()
    def heroes(row):
        return {u["id"] for u in row["units"] if u["id"] in HEROES and u.get("insideCamera") is True}
    def add(cid, row):
        key = (cid, row["file"])
        require(key not in keys, "대표 전장 PNG가 중복되었습니다.")
        chosen.append((cid, boards[cid], row)); keys.add(key); covered.update(heroes(row))
    if explicit is not None:
        require(isinstance(explicit, list) and len(explicit) == 6, "명시적 대표 전장은 정확히 6장이어야 합니다.")
        for entry in explicit:
            cid = entry.get("characterId")
            require(cid in boards, "대표 전장 캐릭터가 선택된 검수 명단과 다릅니다.")
            row = next((r for r in boards[cid]["data"]["rows"] if r["file"] == entry.get("file")), None)
            require(row is not None, "명시한 대표 전장 PNG가 해당 결과에 없습니다.")
            add(cid, row)
    else:
        rotations = (0, 90, 180, 270, 0)
        while set(ids) - covered:
            require(len(chosen) < 5, "대표 5장 안에 동료 전체가 포함되지 않습니다. boards 선택을 명시하세요.")
            angle = rotations[len(chosen)]
            candidates = [(cid, r) for cid, result in boards.items() for r in result["data"]["rows"]
                          if r["phase"] == "idle" and r["width"] == 1280 and r["rotation"] == angle
                          and (cid, r["file"]) not in keys]
            require(candidates, "실제 회전 전장 관측이 없습니다.")
            cid, row = max(candidates, key=lambda item: len((heroes(item[1]) & set(ids)) - covered))
            require((heroes(row) & set(ids)) - covered, "전장 실제 관측만으로 선택 동료 전체를 확인할 수 없습니다.")
            add(cid, row)
        # A partial preview still has real views only; no fabricated missing heroes.
        for angle in (0, 90, 180, 270):
            if len(chosen) >= 5: break
            if any(row["phase"] == "idle" and row["rotation"] == angle for _, _, row in chosen): continue
            candidate = next(((cid, row) for cid, result in boards.items() for row in result["data"]["rows"]
                              if row["phase"] == "idle" and row["width"] == 1280 and row["rotation"] == angle
                              and (cid, row["file"]) not in keys), None)
            if candidate: add(*candidate)
        if len(chosen) < 5:
            cid = next(iter(boards))
            add(cid, next(r for r in boards[cid]["data"]["rows"] if r["phase"] == "character-close" and r["width"] == 1280))
        cid = next(iter(boards))
        add(cid, next(r for r in boards[cid]["data"]["rows"] if r["phase"] == "command-ui" and r["width"] == 2340))
    require(len(chosen) == 6 and set(ids) <= covered, "전장 6장 전체에서 선택된 동료가 모두 보여야 합니다.")
    require(any(r["width"] == 2340 and r["phase"] == "command-ui" for _, _, r in chosen)
            and any(r["rotation"] != 0 for _, _, r in chosen), "대표 전장에는 회전과 가로 명령 UI 관측이 필요합니다.")
    return chosen, [cid for cid in HEROES if cid in covered]


def build_plan(selection, directions, preview):
    data = json.loads(selection.read_text(encoding="utf-8-sig"))
    entries = data.get("characters")
    require(isinstance(entries, list) and entries, "characters 목록이 필요합니다.")
    ids = [row.get("characterId") for row in entries]
    require(len(set(ids)) == len(ids) and set(ids) <= set(HEROES), "캐릭터가 중복되거나 등록 명단과 다릅니다.")
    require(preview or set(ids) == set(HEROES), "정식 갤러리에는 동료 10명 모두 필요합니다. 부분 검토에는 --preview를 사용하세요.")
    sources, public_images, local_images, cache, characters, local_characters, boards = [], {}, {}, {}, [], [], {}
    def record(result, row, side, cid, publish=False):
        category = "board" if side == "board" else "motion"
        destination = f"{category}/{cid}/" + ("" if category == "board" else side + "/") + row["file"]
        if destination not in cache:
            src = image_path(result, row)
            item = {"file":destination, "source":relative(src), "sha256":sha(src), "bytes":src.stat().st_size,
                    "category":category, "characterId":cid, "side":side,
                    "observation":{k:row[k] for k in (FRAME_FIELDS if category == "motion" else BOARD_FIELDS)}}
            if category == "board":
                item["captureWindowUtc"] = {k:result["data"].get(k) for k in ("startedUtc", "finishedUtc")}
                item["observation"]["units"] = [{k:u[k] for k in ("id", "sprite", "action", "viewFacing", "authoredHead", "headScreen", "insideCamera")}
                                               for u in row["units"]]
            cache[destination] = item
        else:
            require(cache[destination]["source"] == relative(image_path(result, row)), "출력 경로가 서로 다른 원본을 가리킵니다.")
        item = cache[destination]
        local_images[destination] = item
        if publish: public_images[destination] = item
        return item
    for entry in sorted(entries, key=lambda r: list(HEROES).index(r["characterId"])):
        cid = entry["characterId"]
        before, after, board = (read_result(entry.get(key)) for key in ("before", "after", "board"))
        validate_motion(before, cid, False); validate_motion(after, cid, True)
        validate_pair(before, after); validate_board(board, cid)
        boards[cid] = board
        for kind, result in (("before", before), ("after", after), ("board", board)):
            sources.append({"characterId":cid, "kind":kind, "file":relative(result["path"]), "sha256":result["sha256"],
                            "unityVersion":result["data"].get("unityVersion"), "passed":True, "complete":True,
                            "authoredIdleOffsetSeconds":result["data"].get("authoredIdleOffsetSeconds"),
                            "startedUtc":result["data"].get("startedUtc"), "finishedUtc":result["data"].get("finishedUtc")})
        item = {"id":cid, "name":HEROES[cid], "snapshots":[]}
        local = {"id":cid, "name":HEROES[cid], "sequences":{}, "blinks":[]}
        for direction in directions:
            local["sequences"][direction] = {}
            for phase in ("idle", "attack", "damage"):
                local["sequences"][direction][phase] = {side:[record(result, row, side, cid) for row in sequence(result, direction, phase)]
                                                       for side, result in (("before", before), ("after", after))}
        for direction in DIRECTIONS:
            blink = next(f for f in after["data"]["frames"] if not f["fullBoard"] and f["viewFacing"] == direction
                         and f["sprite"] == authored(cid, "idle", direction, 2))
            local["blinks"].append(record(after, blink, "after", cid))
        for kind, index, label in (("idle", 0, "개선 후 · 대기"), ("idle", 1, "개선 후 · 들숨"),
                                   ("idle", 2, "개선 후 · 눈감음"), ("attack", 1, "개선 후 · 일반 공격 접촉"),
                                   ("damage", 0, "개선 후 · 피격")):
            expected_phase = "idle" if kind == "idle" else "attack-release" if kind == "attack" else "damage"
            candidates = [f for f in after["data"]["frames"] if not f["fullBoard"] and f["viewFacing"] == "Front"
                          and f["phase"] == expected_phase and f["sprite"] == authored(cid, kind, "Front", index)]
            require(candidates, f"공개 대표 자세의 실제 관측이 없습니다: {cid}/{kind}/{index}. 다른 자세로 대체하지 않습니다.")
            post = candidates[0]
            if kind == "idle" and index == 0:
                pre = min(sequence(before, "Front", "idle"), key=lambda f: abs(f["elapsed"] - post["elapsed"]))
                item["snapshots"].append({"key":"before-idle", "label":"개선 전 · 대기", "image":record(before, pre, "before", cid, True)})
            item["snapshots"].append({"key":f"after-{kind}-{index}", "label":label, "image":record(after, post, "after", cid, True)})
        local["snapshots"] = item["snapshots"]
        characters.append(item); local_characters.append(local)
    chosen, board_ids = select_boards(boards, data.get("boards"), ids)
    public_boards = [record(result, row, "board", cid, True) for cid, result, row in chosen]
    common = {"schema":2, "preview":preview, "sourceImagesEdited":False, "interpolatedFrames":False,
              "sourceResults":sources, "selectionSha256":sha(selection), "limits":LIMITS,
              "boards":public_boards, "boardObservedHeroIds":board_ids}
    public = dict(common, characters=characters, images=list(public_images.values()), scope=SCOPE,
                  presentation="selected-static-observations", continuousPlayback=False)
    local = dict(common, characters=local_characters, images=list(local_images.values()), scope=LOCAL_SCOPE,
                 continuousDirections=directions, presentation="local-original-sequence-playback", continuousPlayback=True)
    require(len(public["images"]) == len(ids) * 6 + 6, "공개 대표 PNG 개수가 1명당 6장 + 전장 6장과 다릅니다.")
    return public, local

SCOPE = "실제 Unity Windows Editor 캡처에서 동료별 개선 전 대기 1장과 개선 후 대기·들숨·눈감음·일반 공격 접촉·피격 5장, 대표 전장 6장을 골랐습니다. 원본 PNG를 그대로 복사했으며, 서로 떨어진 순간을 연속 동작처럼 재생하지 않습니다."
LOCAL_SCOPE = "로컬 검수 전용: 이번 Logs 아래 실제 원본 PNG를 직접 참조합니다. 각 방향·동작의 기록된 연속 관측을 elapsed 시간대로 재생하며, 보충 눈감음은 별도로 표시합니다. 새 이미지 복사·보간·프레임 생성이 없습니다."
LIMITS = "확대 비교는 6화 전장 위 검사용 기본무기 동료 1명, 전장은 해당 동료를 포함한 임시 6명 편성입니다. 알펜의 개선 전은 이미 적용된 기존 시범 원화입니다. PNG 관측 간격은 실행 성능이 아니며, 2340×1080은 Windows Editor 화면 비율 검사입니다. Android 실기기·자연 합류·전투 계산·성장·모든 맵·장시간 성능을 검증한 결과가 아닙니다. 자동 통과와 사람의 시각 승인은 별개입니다."

CSS = """body{margin:0;background:#111a26;color:#e8edf5;font:15px/1.6 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:24px}a{color:#8cddd6}h1{font-size:28px}h2{margin-top:38px}p{max-width:1100px;color:#c3cfdb}nav{display:flex;flex-wrap:wrap;gap:14px}button,select,input{font:inherit}button,select{background:#233951;color:#fff;border:1px solid #55728b;padding:7px 12px;border-radius:6px}button{cursor:pointer}.controls{display:flex;flex-wrap:wrap;align-items:center;gap:9px;margin:12px 0}.controls input{flex:1;min-width:180px}.pair,.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}figure{margin:0;background:#1c293a;border-radius:7px;overflow:hidden}img{display:block;width:100%;height:auto}figcaption{padding:8px 12px}small{display:block;overflow-wrap:anywhere;color:#bdccdb}.blinks{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px}.badge{color:#ffd281}.snapshots{grid-template-columns:repeat(3,minmax(0,1fr))}.overview{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.contact h2{margin-top:10px}.contact .pair{gap:5px}.contact figcaption{font-size:12px}.board{margin-top:12px}details{margin-top:16px}summary{cursor:pointer}progress{width:100%}@media(max-width:800px){.pair,.grid{grid-template-columns:1fr}.blinks{grid-template-columns:repeat(2,1fr)}.overview{grid-template-columns:1fr}.snapshots{grid-template-columns:repeat(2,minmax(0,1fr))}main{padding:14px}}"""

def figure(record, label):
    obs = record["observation"]
    if "elapsed" in obs:
        info = (f"{obs['phase']} · 관측 경과 {obs['elapsed']:.3f}초 · 게임 시간 {obs['time']:.3f}초"
                f" · Unity 프레임 {obs['frame']} · {obs['sprite']}")
        if obs["blinkSupplement"]: info += " · 다음 실제 주기의 별도 보충 관측"
    else:
        info = f"{obs['width']}×{obs['height']} · {obs['phase']} · 회전 {obs['rotation']}° · 6화 전장"
        window = record["captureWindowUtc"]
        info += f" · 캡처 실행 구간 UTC {window['startedUtc']} ~ {window['finishedUtc']} (개별 프레임 시각 미기록)"
    return f'<figure><a href="{html.escape(record["file"], quote=True)}"><img loading="lazy" src="{html.escape(record["file"], quote=True)}" alt="{html.escape(label, quote=True)}"></a><figcaption>{html.escape(label)}<small>{html.escape(info)}</small></figcaption></figure>'


def document(manifest, contact=False):
    local = manifest["continuousPlayback"]
    title = "동료 모션 실제 캡처 " + ("대기 전후 빠른 검수" if contact else "로컬 연속 검수" if local else "대표 장면 비교")
    parts = [f'<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title><style>{CSS}</style><main class="{"contact" if contact else "gallery"}">',
             f'<h1>{title}</h1>', f'<p>{html.escape(manifest["scope"])}</p>', f'<p>{html.escape(LIMITS)}</p>',
             '<p><a href="manifest.json">원본 해시·관측값·검토 범위</a> · <a href="contact.html">전후 대기 빠른 검수</a> · <a href="gallery.html">' + ('로컬 원본 시퀀스' if local else '대표 장면') + '</a></p>']
    if manifest["preview"]:
        parts.append(f'<p class="badge">부분 검토: 실제 완료된 {len(manifest["characters"])}명만 표시합니다. 전체 10명 완료 자료가 아닙니다.</p>')
    parts.append('<nav>' + ''.join(f'<a href="#{c["id"]}">{c["name"]}</a>' for c in manifest["characters"]) + '</nav>')
    if contact: parts.append('<div class="overview">')
    for c in manifest["characters"]:
        parts.append(f'<section id="{c["id"]}"><h2>{c["name"]}</h2>')
        if contact:
            parts.append('<div class="pair">')
            for sample in c["snapshots"][:2]:
                parts.append(figure(sample["image"], c["name"] + " · " + sample["label"]))
            parts.append('</div>')
        elif not local:
            parts.append('<p>정면에서 따로 관측한 순간입니다. 개선 전 대기는 크기·접지 비교 기준이며 개선 후 공격·피격과 같은 동작의 전후 비교가 아닙니다.</p><div class="grid snapshots">')
            for sample in c["snapshots"]:
                parts.append(figure(sample["image"], c["name"] + " · " + sample["label"]))
            parts.append('</div>')
        else:
            parts.append(f'<div class="player" data-character="{c["id"]}"><div class="controls"><select aria-label="방향" class="direction">' + ''.join(f'<option value="{d}">{DIRECTIONS[d]}</option>' for d in c["sequences"]) + '</select><select aria-label="동작" class="phase"><option value="idle">대기</option><option value="attack">일반 공격</option><option value="damage">피격·복귀</option></select><button class="play">재생</button><button class="prev">이전 관측</button><button class="next">다음 관측</button><input class="scrub" aria-label="관측 시간" type="range" min="0" step="any"><span class="clock"></span></div><div class="pair">' + ''.join(f'<figure><img class="{side}" alt="{c["name"]} {label} 실제 관측"><figcaption>{label}<small class="{side}-meta"></small></figcaption></figure>' for side, label in (("before", "개선 전"), ("after", "개선 후"))) + '</div><small class="load"></small></div>')
            parts.append('<details><summary>네 방향 실제 눈감음 관측</summary><p>보충 이미지는 첫 대기 촬영 뒤 실제 다음 주기에서 따로 관측했습니다. 연속 시퀀스에 끼워 넣지 않았습니다.</p><div class="blinks">')
            for record in c["blinks"]:
                obs = record["observation"]
                parts.append(figure(record, c["name"] + " · " + DIRECTIONS[obs["viewFacing"]] + (" · 별도 보충 관측" if obs["blinkSupplement"] else " · 연속 관측 안의 프레임")))
            parts.append('</div></details>')
        parts.append('</section>')
    if contact: parts.append('</div>')
    else:
        parts.append('<section id="boards"><h2>실제 대표 전장 6장</h2><p>각 장면은 해당 동료를 포함한 임시 6명 편성입니다. 여섯 장 전체에서 관측된 동료: ' + ', '.join(HEROES[cid] for cid in manifest["boardObservedHeroIds"]) + '.</p><div class="grid">')
        for record in manifest["boards"]:
            parts.append(figure(record, HEROES[record["characterId"]] + ' 포함 편성 · ' + record["observation"]["phase"]))
        parts.append('</div></section>')
    if local and not contact:
        safe_json = json.dumps(manifest["characters"], ensure_ascii=False, separators=(",", ":")).replace("<", "\\u003c").replace(">", "\\u003e").replace("&", "\\u0026")
        parts.append('<script type="application/json" id="data">' + safe_json + '</script><script>' + JS + '</script>')
    parts.append('</main></html>')
    return '\n'.join(parts)


JS = r"""'use strict';
const characters=JSON.parse(document.getElementById('data').textContent), players=[];
for(const el of document.querySelectorAll('.player')){
 const c=characters.find(x=>x.id===el.dataset.character), dir=el.querySelector('.direction'), phase=el.querySelector('.phase'), scrub=el.querySelector('.scrub'), play=el.querySelector('.play');
 let t=0,last=0,running=false,loaded=false,token=0,seq,max=0,marks=[];
 const stop=()=>{running=false;play.textContent='재생';};players.push(stop);
 function show(){
  for(const side of ['before','after']){
   const frames=seq[side];let item=frames[0];for(const f of frames){if(f.observation.elapsed<=t)item=f;else break;}
   const obs=item.observation,img=el.querySelector('img.'+side);if(img.getAttribute('src')!==item.file)img.src=item.file;
   const hold=t>frames.at(-1).observation.elapsed?' · 마지막 관측 유지':t<frames[0].observation.elapsed?' · 첫 관측 이전: 첫 실제 관측 표시':'';
   el.querySelector('.'+side+'-meta').textContent=`관측 ${obs.elapsed.toFixed(3)}초 · Unity 프레임 ${obs.frame} · ${obs.phase} · ${obs.sprite}${hold}`;
  }
  scrub.value=String(t);el.querySelector('.clock').textContent=t.toFixed(3)+' / '+max.toFixed(3)+'초';
 }
 async function load(){
  stop();loaded=false;play.disabled=true;const n=++token;seq=c.sequences[dir.value][phase.value];max=Math.max(...Object.values(seq).map(a=>a.at(-1).observation.elapsed));scrub.max=String(max);t=0;
  marks=[...new Set(Object.values(seq).flat().map(f=>f.observation.elapsed))].sort((a,b)=>a-b);show();
  el.querySelector('.load').textContent='현재 시퀀스의 실제 PNG를 불러오는 중…';
  const urls=[...new Set(Object.values(seq).flat().map(f=>f.file))];
  const results=await Promise.all(urls.map(src=>new Promise(resolve=>{const im=new Image();im.onload=()=>resolve(true);im.onerror=()=>resolve(false);im.src=src;})));
  if(n!==token)return;
  loaded=results.every(Boolean);play.disabled=!loaded;
  el.querySelector('.load').textContent=loaded?'관측 elapsed 시간으로 재생합니다. 보간 없음 · 전후 종료 시간이 다르면 마지막 실제 관측을 유지합니다.':'실제 PNG를 읽지 못했습니다. 이미지 파일이 함께 있는지 확인하세요.';
 }
 play.onclick=()=>{if(!loaded)return;if(running){stop();return;}players.forEach(f=>f());if(t>=max)t=0;running=true;last=performance.now();play.textContent='일시 정지';};
 scrub.oninput=()=>{stop();t=Number(scrub.value);show();};
 el.querySelector('.prev').onclick=()=>{stop();t=[...marks].reverse().find(v=>v<t-.0001)??0;show();};
 el.querySelector('.next').onclick=()=>{stop();t=marks.find(v=>v>t+.0001)??max;show();};
 dir.onchange=phase.onchange=load;
 function tick(now){if(running){t=Math.min(max,t+(now-last)/1000);last=now;show();if(t>=max)stop();}requestAnimationFrame(tick);}requestAnimationFrame(tick);
 document.addEventListener('visibilitychange',()=>{if(document.hidden)stop();});
 seq=c.sequences[dir.value][phase.value];max=Math.max(...Object.values(seq).map(a=>a.at(-1).observation.elapsed));scrub.max=String(max);show();
 // Keep the ten actors from decoding every sequence simultaneously on page load.
 const observer=new IntersectionObserver(entries=>{if(entries.some(e=>e.isIntersecting)){observer.disconnect();load();}},{rootMargin:'200px'});observer.observe(el);
}
"""

def local_links(manifest, output):
    """Local-only HTML uses original captures; no raster resize, transcode or copying."""
    result = copy.deepcopy(manifest)
    def visit(value):
        if isinstance(value, dict):
            if "source" in value and "sha256" in value and "file" in value:
                path = (ROOT / value["source"]).resolve()
                require(inside(path, LOG), "로컬 원본 참조가 이번 Logs 밖입니다.")
                value["file"] = Path(os.path.relpath(path, output)).as_posix()
            for child in value.values(): visit(child)
        elif isinstance(value, list):
            for child in value: visit(child)
    visit(result)
    return result


def write_documents(output, manifest):
    (output / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding="utf-8")
    (output / "gallery.html").write_text(document(manifest), encoding="utf-8")
    (output / "contact.html").write_text(document(manifest, True), encoding="utf-8")
    (output / "manifest.sha256").write_text(sha(output / "manifest.json") + "  manifest.json\n", encoding="ascii")
    local = manifest["continuousPlayback"]
    (output / "README.md").write_text("# 동료 모션 실제 캡처 검토\n\n" + manifest["scope"] + "\n\n" + LIMITS +
        "\n\n[" + ("로컬 원본 시퀀스" if local else "대표 장면") + "](gallery.html) · [전후 대기 빠른 검수](contact.html) · [관측값·원본 해시](manifest.json)\n\n" +
        f"이번 자료: {len(manifest['characters'])}명. " +
        ("PNG 복사 없이 Logs 원본을 참조하므로 이 폴더만 공개하거나 이동하면 재생되지 않습니다.\n" if local else
         f"선택 원본 PNG {len(manifest['images'])}개. 선택된 순간을 연속 애니메이션으로 재생하지 않습니다.\n") +
        ("부분 검토본이며 전체 10명 완료를 뜻하지 않습니다.\n" if manifest["preview"] else "동료 10명의 실제 캡처 자료입니다.\n"), encoding="utf-8")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--selection", type=Path, required=True)
    ap.add_argument("--output", type=Path, default=EVIDENCE)
    ap.add_argument("--local-output", type=Path, help="새 Logs 하위 폴더. 원본 참조 연속 검수만 저장하고 PNG는 복사하지 않습니다.")
    ap.add_argument("--local-only", action="store_true", help="공개 Evidence를 쓰지 않고 로컬 HTML/manifest만 만듭니다.")
    ap.add_argument("--directions", nargs="+", choices=list(DIRECTIONS), default=list(DIRECTIONS), help="로컬 연속 검수 방향. 공개 대표 장면은 정면입니다.")
    ap.add_argument("--preview", action="store_true")
    ap.add_argument("--check-only", action="store_true")
    ap.add_argument("--max-png-bytes", type=int, help="공개 PNG 총량 상한. 검사와 복사 모두 이 값을 넘으면 중단합니다.")
    args = ap.parse_args()
    output = args.output.resolve()
    local_output = args.local_output.resolve() if args.local_output else None
    require(inside(output, EVIDENCE), "공개 출력은 이번 Evidence 폴더 안으로 제한됩니다.")
    require(local_output is None or (inside(local_output, LOG) and local_output != LOG), "로컬 출력은 이번 Logs 안의 새 하위 폴더여야 합니다.")
    require(not args.local_only or local_output is not None, "--local-only에는 --local-output이 필요합니다.")
    require(len(set(args.directions)) == len(args.directions), "방향이 중복되었습니다.")
    if not args.check_only:
        require(args.local_only or not output.exists(), "기존 공개 증거를 덮어쓰지 않습니다. Evidence 아래 새 출력 폴더를 지정하세요.")
        require(local_output is None or not local_output.exists(), "기존 로컬 증거를 덮어쓰지 않습니다. 새 출력 폴더를 지정하세요.")
    public, local = build_plan(args.selection.resolve(), args.directions, args.preview)
    for manifest in (public, local):
        require(re.search(r'[A-Za-z]:[\\/]|file://|/Users/|/home/', json.dumps(manifest, ensure_ascii=False)) is None,
                "자료에 절대 개인 경로가 포함됐습니다.")
    png_bytes = sum(x["bytes"] for x in public["images"])
    require(args.max_png_bytes is None or (args.max_png_bytes >= 0 and png_bytes <= args.max_png_bytes),
            f"공개 PNG {png_bytes}바이트가 지정한 상한을 넘었습니다.")
    summary = {"passed":True, "checkOnly":args.check_only, "preview":args.preview, "characters":len(public["characters"]),
               "publicPngCount":len(public["images"]), "publicPngBytes":png_bytes,
               "publicCharacterPngCount":len(public["images"]) - len(public["boards"]),
               "publicBoardPngCount":len(public["boards"]), "publicBoardPngBytes":sum(x["bytes"] for x in public["boards"]),
               "perCharacterPublicPngBytes":{c["id"]:sum(s["image"]["bytes"] for s in c["snapshots"]) for c in public["characters"]},
               "boardObservedHeroIds":public["boardObservedHeroIds"],
               "localReferencedPngCount":len(local["images"]), "localReferencedPngBytes":sum(x["bytes"] for x in local["images"]),
               "localContinuousFrameCount":sum(len(frames) for c in local["characters"] for direction in c["sequences"].values()
                                                for phase in direction.values() for frames in phase.values()),
               "localPngCopies":0, "localDirections":args.directions, "publicOutput":None if args.local_only else relative(output),
               "localOutput":relative(local_output) if local_output else None}
    if args.check_only:
        print(json.dumps(summary, ensure_ascii=False)); return
    for result in public["sourceResults"]:
        require(sha(ROOT / result["file"]) == result["sha256"], "검증 중 결과 JSON이 변경됐습니다. 아직 출력하지 않았습니다.")
    if local_output:
        for record in local["images"]:
            require(sha(ROOT / record["source"]) == record["sha256"], "검증 중 실제 PNG가 변경됐습니다. 아직 출력하지 않았습니다.")
    if not args.local_only:
        output.mkdir(parents=True)
        for record in public["images"]:
            src, dest = ROOT / record["source"], output / record["file"]
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(src, dest)
            require(sha(dest) == record["sha256"], "복사 중 원본 PNG가 변경됐습니다. 불완전 출력은 보존했습니다.")
        public["publication"] = {"pngCount":len(public["images"]), "pngBytes":png_bytes, "allSourceCopiesSha256Matched":True,
                                 "sequenceFramesPublished":False, "audioOrBuildFilesPublished":False}
        write_documents(output, public)
        summary["publicManifestSha256"] = sha(output / "manifest.json")
    if local_output:
        local_output.mkdir(parents=True)
        local = local_links(local, local_output)
        local["localReview"] = {"pngCopies":0, "originalPngReferencesSha256Checked":True, "publication":False}
        write_documents(local_output, local)
        summary["localManifestSha256"] = sha(local_output / "manifest.json")
    print(json.dumps(summary, ensure_ascii=False))

if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError, TypeError, StopIteration, json.JSONDecodeError) as error:
        print(json.dumps({"passed":False, "error":str(error)}, ensure_ascii=False), file=sys.stderr)
        sys.exit(1)
