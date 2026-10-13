"""Build the offline design catalog. No game assets or runtime code are touched."""
from pathlib import Path
from collections import Counter
import hashlib
import html
import json
import struct

ROOT = Path(__file__).resolve().parent
data = json.loads((ROOT / "roster.json").read_text(encoding="utf-8"))
bosses = data["bosses"]
groups = {"phantasia": "판타지아", "eternia": "이터니아", "abyss": "어비스",
          "berseria": "베르세리아", "arise": "어라이즈", "originals": "창작 보스"}
esc = html.escape
by_id = {b["id"]: b for b in bosses}
assert len(by_id) == len(bosses) == 20
assert Counter(b["rank"] for b in bosses) == {"주요 보스": 6, "중간보스": 12, "시련 보스": 2}
assert [c["number"] for c in data["chapters"]] == list(range(1, 21))
assert all(len(b["skills"]) == 3 for b in bosses)
assert all(s[1] in ("원작", "창작") for b in bosses for s in b["skills"])
placed = {i for c in data["chapters"] for i in c["bossIds"]}
assert placed == {b["id"] for b in bosses if b["rank"] != "시련 보스"}
assert all(c["number"] in by_id[i]["chapters"] for c in data["chapters"] for i in c["bossIds"])
assert all(any(c["number"] == n and b["id"] in c["bossIds"] for c in data["chapters"])
           for b in bosses for n in b["chapters"])

gallery, images = [], []
for group, label in groups.items():
    filename = f"concept-{group}.png"
    raw = (ROOT / filename).read_bytes()
    assert raw[:8] == b"\x89PNG\r\n\x1a\n"
    width, height = struct.unpack(">II", raw[16:24])
    assert width >= 1024 and height >= 768
    names = " · ".join(b["name"] for b in bosses if b["group"] == group)
    gallery.append(f'<figure id="art-{group}"><a href="{filename}"><img src="{filename}" '
                   f'width="{width}" height="{height}" alt="{esc(label)} 모델 시안: {esc(names)}"></a>'
                   f'<figcaption><strong>{label}</strong><span>{esc(names)}</span></figcaption></figure>')
    images.append({"path": filename, "width": width, "height": height,
                   "sha256": hashlib.sha256(raw).hexdigest()})

cards = []
for b in bosses:
    skills = "".join(f'<li><div><strong>{esc(s[0])}</strong><span class="tag {"original" if s[1] == "원작" else "new"}">{s[1]}명</span></div><p>{esc(s[2])}</p></li>' for s in b["skills"])
    sources = " · ".join(f'<a href="{esc(link)}" target="_blank" rel="noopener noreferrer">원작 참고 {i+1}</a>' for i, link in enumerate(b["links"]))
    when = " · ".join(f"{n}화" for n in b["chapters"]) or b["unlock"]
    search = esc(" ".join([b["name"], b["en"], b["role"], b["hero"], groups[b["group"]]]).lower(), quote=True)
    cards.append(f'''<article class="boss" id="{b["id"]}" data-rank="{b["rank"]}" data-group="{b["group"]}" data-search="{search}">
<div class="eyebrow">{groups[b["group"]]} <span> / {b["rank"]}</span></div>
<h3>{esc(b["name"])}</h3><div class="english">{esc(b["en"])}</div>
<p class="when">{esc(when)} · {esc(b["hero"])}</p><p>{esc(b["role"])}</p>
<p class="model"><b>모델 방향</b> {esc(b["visual"])} <a href="#art-{b["group"]}">시안 보기 ↗</a></p>
<ul class="skills">{skills}</ul>
<p class="counter"><b>공략</b> {esc(b["counter"])}</p>
<details><summary>페이즈·원작 구분·참고 자료</summary><p>{esc(b["phase"])}</p><p>{esc(b["notes"])}</p><p>{sources or "본 프로젝트 창작 설정"}</p></details>
</article>''')

rows = []
for c in data["chapters"]:
    names = " / ".join(f'<a href="#{i}">{esc(by_id[i]["name"])}</a>' for i in c["bossIds"])
    rows.append(f'<tr><td>{c["number"]:02}</td><td>{esc(c["title"])}</td><td>{names}</td><td>{esc(c["flow"])}</td></tr>')

css = """
:root{color-scheme:dark;--bg:#0b1420;--panel:#142330;--line:#32424c;--ink:#edf1f3;--muted:#b4c2cd;--gold:#e6bd7a;--teal:#8edbd0}
*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:24px}
body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.75 "Segoe UI","Malgun Gothic",sans-serif}
main{max-width:1440px;margin:auto;padding:44px 32px 80px}a{color:var(--teal);text-underline-offset:4px}
header{padding:20px 0 34px;border-bottom:1px solid var(--line)}h1{font-size:42px;line-height:1.25;margin:16px 0}h2{font-size:27px;margin:50px 0 20px}h3{font-size:25px;margin:9px 0 0}
p{margin:10px 0}.kicker,.eyebrow{font-size:13px;color:var(--gold);letter-spacing:1px}.eyebrow span{color:var(--muted)}
.intro{max-width:880px;color:var(--muted)}.notice{border-left:3px solid var(--gold);padding:12px 18px;background:#192733;margin-top:24px}
.stats{display:flex;gap:36px;flex-wrap:wrap;margin:25px 0}.stats strong{font-size:32px;color:var(--gold);margin-right:8px}
nav{display:flex;gap:20px;flex-wrap:wrap}.gallery{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px}
figure{margin:0;background:var(--panel);border:1px solid var(--line);border-radius:10px;overflow:hidden}
figure img{display:block;width:100%;height:auto}figcaption{padding:14px 18px}figcaption span{display:block;font-size:13px;color:var(--muted)}
.legend{color:var(--muted);max-width:980px}.controls{display:flex;gap:12px;flex-wrap:wrap;margin:22px 0}
input,select{background:#162836;color:var(--ink);border:1px solid #566a78;border-radius:6px;padding:12px 14px;font:inherit;max-width:100%}
input{min-width:240px;flex:1}label{display:flex;align-items:center;gap:10px}.controls label:first-child{flex:1}
#count{color:var(--gold);margin:10px 0}.cards{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:22px;align-items:start}
.boss{padding:27px;background:var(--panel);border:1px solid var(--line);border-radius:10px;min-width:0}
.boss:target{outline:2px solid var(--gold);outline-offset:4px}.english{color:var(--muted);font-size:13px}
.when{font-size:13px;color:var(--teal)}.model{font-size:14px;padding:12px 0;border-top:1px solid var(--line);border-bottom:1px solid var(--line)}
.skills{padding:0;list-style:none}.skills li{margin:17px 0}.skills li div{display:flex;gap:10px;align-items:center;flex-wrap:wrap}.skills p{font-size:14px;color:var(--muted);margin:3px 0}
.tag{font-size:11px;padding:0 7px;border-radius:3px;background:#223d48;color:var(--teal)}.tag.new{background:#433921;color:var(--gold)}
.counter{font-size:14px;background:#20333d;padding:12px;border-radius:5px}details{font-size:13px;color:var(--muted);margin-top:18px}summary{cursor:pointer;color:var(--teal)}
.table-wrap{overflow:auto;border:1px solid var(--line);border-radius:8px}table{border-collapse:collapse;width:100%;font-size:14px}th,td{padding:14px;text-align:left;border-bottom:1px solid var(--line);vertical-align:top}th{color:var(--gold);background:var(--panel)}td:first-child{color:var(--gold)}td:nth-child(2){min-width:185px}td:nth-child(3){min-width:150px}
.rules{max-width:1000px}.rules li{margin:14px 0;color:var(--muted)}footer{border-top:1px solid var(--line);padding-top:24px;margin-top:50px;font-size:13px;color:var(--muted)}
[hidden]{display:none!important}:focus-visible{outline:3px solid var(--gold);outline-offset:4px}
@media(max-width:760px){main{padding:24px 16px 50px}h1{font-size:31px}.gallery,.cards{grid-template-columns:1fr}.boss{padding:21px}.controls{flex-direction:column}.controls label{justify-content:space-between}input{min-width:0;width:100%}.stats{gap:20px}.stats strong{font-size:27px}}
@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}}
@media print{body{background:white;color:black}.controls,nav{display:none}.boss,figure{break-inside:avoid}a,.counter,.model,.legend,.intro,.rules li{color:black}main{padding:0}}
"""
js = """
const search=document.querySelector('#search'),rank=document.querySelector('#rank'),group=document.querySelector('#group');
function filter(){let count=0;const q=search.value.toLowerCase().trim();document.querySelectorAll('.boss').forEach(card=>{const visible=(!q||card.dataset.search.includes(q))&&(!rank.value||card.dataset.rank===rank.value)&&(!group.value||card.dataset.group===group.value);card.hidden=!visible;if(visible)count++;});document.querySelector('#count').textContent=count+' / 20종 표시';document.querySelector('#empty').hidden=count!==0;}
[search,rank,group].forEach(el=>el.addEventListener('input',filter));
function revealHash(){const el=document.getElementById(location.hash.slice(1));if(el&&el.classList.contains('boss')){search.value='';rank.value='';group.value='';filter();el.scrollIntoView();}}
window.addEventListener('hashchange',revealHash);filter();revealHash();
"""
options = "".join(f'<option value="{key}">{value}</option>' for key, value in groups.items())
rules = "".join(f"<li>{esc(r)}</li>" for r in data["rules"])
notes = "".join(f"<li>{esc(r)}</li>" for r in data["productionNotes"])
document = f'''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>TalesTactics · 보스 20종 확장안</title><style>{css}</style></head><body><main>
<header><div class="kicker">TALES TACTICS / CHARACTER & ENCOUNTER DESIGN / 2026.10.13</div>
<h1>경계를 잇는 의지<br>보스 20종 확장안</h1>
<p class="intro">다섯 작품의 적과 라이벌이 각자의 목적을 갖고 충돌합니다. 거대한 적을 피하는 전투부터 검술 대결, 분신 격파, 시전 방해, 동료 구출, 정령의 계약까지 — 외형과 공략 경험을 함께 설계했습니다.</p>
<div class="stats"><div><strong>6</strong>주요 보스</div><div><strong>12</strong>중간보스</div><div><strong>2</strong>시련 보스</div><div><strong>20</strong>메인 시나리오</div></div>
<nav aria-label="목차"><a href="#models">모델 시안 6장</a><a href="#bosses">캐릭터·스킬</a><a href="#flow">20화 배치</a><a href="#rules">공통 전투 규칙</a></nav>
<p class="notice"><b>기획 제안 · 게임 미적용</b><br>{esc(data["summary"])} 현재 출시판은 6화 캠페인이며, 이 문서의 20화·신규 보스·기술 효과는 구현 완료를 뜻하지 않습니다.</p></header>
<section id="models"><h2>모델 시안</h2><p class="legend">{esc(data["artNote"])} 이미지를 선택하면 원본 크기로 볼 수 있습니다.</p>
<div class="gallery">{"".join(gallery)}</div></section>
<section id="bosses"><h2>캐릭터 · 사용 스킬 · 공략 특징</h2><p class="legend">{esc(data["sourcesNote"])}</p>
<div class="controls"><label for="search">검색<input id="search" type="search" placeholder="이름 · 작품 · 연관 아군"></label>
<label for="rank">등급<select id="rank"><option value="">전체</option><option>주요 보스</option><option>중간보스</option><option>시련 보스</option></select></label>
<label for="group">작품<select id="group"><option value="">전체</option>{options}</select></label></div>
<p id="count" role="status" aria-live="polite"></p><p id="empty" hidden>검색 조건에 맞는 캐릭터가 없습니다.</p><div class="cards">{"".join(cards)}</div></section>
<section id="flow"><h2>20화 메인 플로우</h2><p class="legend">1~5화 합류 → 6~10화 구원의 대가 → 11~16화 진실과 선택 → 17~20화 반격과 귀환. 오리진·맥스웰은 14~16화 사이 선택 시련입니다.</p>
<div class="table-wrap"><table><thead><tr><th scope="col">화</th><th scope="col">시나리오</th><th scope="col">보스</th><th scope="col">진행·승리 조건</th></tr></thead><tbody>{"".join(rows)}</tbody></table></div></section>
<section id="rules"><h2>보스전 공통 규칙</h2><ul class="rules">{rules}</ul><h2>제작 범위와 후속 과제</h2><ul class="rules">{notes}</ul></section>
<footer>AI 이미지 생성: 내장 imagegen · 상세 프롬프트와 채택 파일: <a href="generation.json">generation.json</a> · 기획 데이터: <a href="roster.json">roster.json</a><br>검증: <a href="validation.json">자료 무결성</a> · <a href="browser-review.json">문서 화면·필터 검사</a>. 이 검사는 Unity 테스트가 아닙니다.</footer>
</main><script>{js}</script></body></html>'''
(ROOT / "proposal.html").write_text(document, encoding="utf-8")
result = {"status": "passed", "scope": "offline design catalog only; not Unity tests",
          "bossCount": 20, "rankCounts": dict(Counter(b["rank"] for b in bosses)),
          "skillCount": sum(len(b["skills"]) for b in bosses), "mainChapters": 20,
          "checks": ["unique identities", "rank totals", "three named skills per boss",
                     "all non-trial bosses scheduled", "bidirectional chapter references",
                     "PNG signatures, dimensions and SHA256", "HTML generated from roster"],
          "images": images, "limitations": data["productionNotes"],
          "visualReview": "Six concept sheets visually inspected; fan interpretation, not production sprites. Final 3-head proportions, accessory consistency and four-direction turnarounds remain production work."}
(ROOT / "validation.json").write_text(json.dumps(result, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print(json.dumps({"status": "passed", "bosses": 20, "skills": 60, "chapters": 20, "images": len(images)}, ensure_ascii=False))
