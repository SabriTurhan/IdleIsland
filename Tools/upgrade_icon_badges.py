import sys
from PIL import Image, ImageDraw, ImageFilter
S=4  # supersample
OUT=(48,48,48,255)
def stack_badge(size=240):
    W=size*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
    w=int(W*0.78); x0=(W-w)//2; eh=int(W*0.2); lw=int(W*0.045)
    layers=3; step=int(W*0.2); top=int(W*0.12)
    for i in reversed(range(layers)):
        y=top+i*step
        # body
        d.rectangle([x0,y+eh//2,x0+w,y+eh//2+step],fill=OUT)
        d.ellipse([x0,y+step,x0+w,y+step+eh],fill=OUT)
        d.rectangle([x0+lw,y+eh//2,x0+w-lw,y+eh//2+step],fill=(86,140,230,255))
        d.ellipse([x0+lw,y+step+lw//2,x0+w-lw,y+step+eh-lw],fill=(70,118,214,255))
        # top
        d.ellipse([x0,y,x0+w,y+eh+lw],fill=OUT)
        d.ellipse([x0+lw,y+lw,x0+w-lw,y+eh],fill=(150,192,255,255))
        d.ellipse([x0+int(w*0.18),y+int(eh*0.3),x0+int(w*0.45),y+int(eh*0.55)],fill=(210,230,255,255))
    return im.resize((size,size),Image.LANCZOS)
def bolt_badge(size=240):
    W=size*S; im=Image.new('RGBA',(W,W),(0,0,0,0)); d=ImageDraw.Draw(im)
    pts=[(0.58,0.04),(0.18,0.56),(0.46,0.56),(0.36,0.96),(0.82,0.40),(0.54,0.40),(0.70,0.04)]
    P=[(x*W,y*W) for x,y in pts]
    d.polygon(P,fill=OUT)
    # inner (shrink toward centroid)
    cx=sum(p[0] for p in P)/len(P); cy=sum(p[1] for p in P)/len(P)
    inner=[(cx+(x-cx)*0.8,cy+(y-cy)*0.86) for x,y in P]
    d.polygon(inner,fill=(255,190,40,255))
    hl=[(cx+(x-cx)*0.55-W*0.03,cy+(y-cy)*0.6-W*0.04) for x,y in P]
    d.polygon(hl,fill=(255,226,110,255))
    return im.resize((size,size),Image.LANCZOS)
def compose(product,badge,out):
    base=Image.new('RGBA',(512,512),(0,0,0,0))
    p=Image.open(product).convert('RGBA').resize((430,430),Image.LANCZOS)
    base.alpha_composite(p,(0,10))
    b=stack_badge() if badge=='cap' else bolt_badge()
    base.alpha_composite(b,(512-b.width-6,512-b.height-6))
    base.save(out)
if __name__=='__main__':
    compose(sys.argv[1],sys.argv[2],sys.argv[3])
