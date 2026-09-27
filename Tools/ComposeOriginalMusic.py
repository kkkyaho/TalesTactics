"""Original score and offline synthesizer. No samples/downloads or existing song melodies.
Writes new WAVs only; existing audio is never overwritten. NumPy required.
"""
from pathlib import Path
import hashlib,json,wave
import numpy as np

RATE=44100
ROOT=Path('Assets/TalesTactics/Audio/Original')
# id, tempo, MIDI tonic, instrument, original eight-note phrase, chord roots, bars
SCORES=[
 ('battle',132,62,'brass',[0,2,3,7,5,3,2,0],[0,-4,3,-2],16),
 ('boss',152,57,'brass',[0,7,6,3,2,6,7,12],[0,-1,-4,-2],16),
 ('story',84,65,'piano',[0,4,7,9,7,4,2,0],[0,5,-3,7],8),
 ('victory',144,62,'bell',[0,4,7,12,11,9,7,12],[0,5,7,0],4),
 ('cless.theme',132,62,'brass',[0,7,12,10,7,5,3,7],[0,3,-4,-2],4),
 ('mint.theme',96,65,'bell',[0,4,7,11,9,7,4,2],[0,5,-3,7],4),
 ('velvet.theme',144,57,'pluck',[0,3,1,7,6,3,7,12],[0,-1,-4,-2],4),
 ('farah.theme',148,67,'flute',[0,2,7,4,9,7,4,2],[0,5,7,0],4),
 ('tear.theme',100,69,'flute',[0,3,7,10,12,10,7,3],[0,-4,3,-2],4),
 ('jade.theme',120,59,'piano',[0,7,3,10,9,6,3,2],[0,-4,-1,-2],4),
 ('natalia.theme',124,64,'pluck',[0,4,9,7,11,9,7,4],[0,5,-3,7],4),
 ('alphen.theme',140,62,'brass',[0,3,7,12,14,12,10,7],[0,-4,3,-2],4),
 ('shionne.theme',112,66,'bell',[0,3,10,7,12,10,7,2],[0,-4,3,-2],4),
 ('kisara.theme',116,60,'brass',[0,7,5,3,7,10,7,0],[0,3,-4,-2],4),
]

def voice(midi,duration,instrument):
    t=np.arange(int(duration*RATE))/RATE
    f=440*2**((midi-69)/12);phase=2*np.pi*f*t
    attack=.025 if instrument=='pad' else .008
    envelope=np.minimum(1,t/attack)*np.minimum(1,(duration-t)/.08)
    if instrument=='piano':
        v=sum(np.sin(phase*k)*np.exp(-t*(2+k*.5))/(k*k) for k in range(1,6))
    elif instrument=='bell':v=np.sin(phase+1.4*np.sin(phase*2)*np.exp(-t*5))*np.exp(-t*2.4)
    elif instrument=='pluck':v=(np.sin(phase)+.28*np.sin(phase*2)+.12*np.sin(phase*3))*np.exp(-t*4)
    elif instrument=='flute':v=np.sin(phase+.015*np.sin(t*2*np.pi*5))+.12*np.sin(phase*2)
    elif instrument=='brass':v=sum(np.sin(phase*k)/k**1.8 for k in range(1,7))*(.7+.3*np.exp(-t*9))
    else:v=np.sin(phase)+.22*np.sin(phase*2+.025*np.sin(t*5))
    return v*envelope

def compose(score,index):
    id,bpm,key,instrument,motif,roots,bars=score;beat=60/bpm;duration=bars*4*beat
    n=round(duration*RATE);mix=np.zeros((n,2));rng=np.random.default_rng(734+index)
    loop=id!='victory';calm=id in ['story','mint.theme','tear.theme'];major=id in ['story','victory','mint.theme','farah.theme','natalia.theme']
    def add(signal,start,gain,pan=0):
        at=round(start*RATE);length=len(signal);pos=(np.arange(length)+at)
        if loop:pos%=n
        else:signal=signal[pos<n];pos=pos[pos<n]
        stereo=signal[:,None]*np.array([np.sqrt((1-pan)/2),np.sqrt((1+pan)/2)])[None,:]*gain
        np.add.at(mix,pos,stereo)
    for bar in range(bars):
        root=key+roots[bar%4];chord=[root,root+(4 if major else 3),root+7]
        start=bar*4*beat
        for k,note in enumerate(chord):add(voice(note,4.2*beat,'pad'),start,.055,(k-1)*.6)
        for step in range(8):
            onset=start+step*.5*beat
            degree=motif[(step+(bar//4)*2)%8]
            note=key+12+degree+(12 if bar%8==7 and step>=6 else 0)
            add(voice(note,.46*beat,instrument),onset,.22,-.08)
            add(voice(chord[step%3]+12,.38*beat,'pluck'),onset,.07,.5)
            if step%2==0:add(voice(root-12 if step%4==0 else root-5,.88*beat,'pluck'),onset,.24,-.2)
        if not calm:
            for b in range(4):
                t=np.arange(int(.18*RATE))/RATE
                if b%2==0:add(np.sin(2*np.pi*(48*t+1.8*(1-np.exp(-t*35))))*np.exp(-t*26),start+b*beat,.35)
                else:add(rng.standard_normal(len(t))*np.exp(-t*32),start+b*beat,.09,.12)
            for h in range(8):
                noise=rng.standard_normal(int(.045*RATE));noise=np.diff(noise,prepend=0)*np.exp(-np.arange(len(noise))/RATE*90)
                add(noise,start+h*.5*beat,.025,.65)
    # Short, subtle stereo echo; circular delay preserves loop tails.
    delay=round(beat*.75*RATE)
    if loop:mix+=np.roll(mix,delay,axis=0)[:,::-1]*.16
    else:
        mix[delay:]+=mix[:-delay,::-1]*.16
        fade=min(n,round(.6*RATE));mix[-fade:]*=np.linspace(1,0,fade)[:,None]
    mix-=mix.mean(axis=0)
    mix=np.tanh(mix*1.2);mix*=.72/max(.72,np.max(np.abs(mix)))
    # Sub-millisecond boundary taper prevents hard sample discontinuity on loops.
    fade=round(.004*RATE);mix[:fade]*=np.linspace(0,1,fade)[:,None];mix[-fade:]*=np.linspace(1,0,fade)[:,None]
    return np.round(mix*32767).astype('<i2')

ROOT.mkdir(parents=True,exist_ok=True);manifest=[]
for i,score in enumerate(SCORES):
    id=score[0];path=ROOT/(id+'.wav')
    if not path.exists():
        samples=compose(score,i)
        with wave.open(str(path),'wb') as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(samples.tobytes())
    with wave.open(str(path),'rb') as f:
        samples=np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').astype(float)/32768
        item={'id':id,'file':str(path).replace('\\','/'),'seconds':f.getnframes()/f.getframerate(),'channels':f.getnchannels(),'rate':f.getframerate(),'peak':float(np.max(np.abs(samples))),'rms':float(np.sqrt(np.mean(samples*samples))),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
    assert .01<item['rms']<.3 and item['peak']<.9,item
    manifest.append(item);print(id,round(item['seconds'],2),'seconds',flush=True)
Path('Docs/original-music-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
