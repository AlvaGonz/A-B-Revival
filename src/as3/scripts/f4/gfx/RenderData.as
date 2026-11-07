package f4.gfx
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.unit.ai.AIUnity;
   import f4.unit.render.BGMoveSimple;
   import f4.unit.render.Cam;
   import f4data.sdata.ConfigAndStory;
   import flash.display.Bitmap;
   import flash.display.BitmapData;
   import flash.display.DisplayObjectContainer;
   import flash.display.PixelSnapping;
   import flash.display.Sprite;
   import flash.display.Stage;
   import flash.display.Stage3D;
   import flash.events.Event;
   import flash.filters.BlurFilter;
   import flash.geom.ColorTransform;
   import flash.geom.Matrix;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.tool.SimpleGlowFilter;
   
   public class RenderData
   {
      
      public var CANVAS:BitmapData;
      
      public var BMPD:BitmapData;
      
      public var BMPDFX:BitmapData;
      
      public var BMPDBLTFX:BitmapData;
      
      public var BMPDBG:BitmapData;
      
      public var MINIMAP:BitmapData;
      
      public var MTX2X:Matrix;
      
      public var UISP:Sprite;
      
      public var W:int = 1920;
      
      public var H:int = 1080;
      
      public var FACE:int = 64;
      
      public var PIDFACE:Number = 0.09817477042468103;
      
      public var renderwh:Point;
      
      public var cam:Cam;
      
      public var drawbg:BGMoveSimple;
      
      public var fxrect:Rectangle;
      
      public var point:Point = new Point();
      
      public var ct:ColorTransform = new ColorTransform(1,1,1,0.6);
      
      public var fxct:ColorTransform = new ColorTransform(1,1,1,0.55);
      
      public var fxctblt:ColorTransform = new ColorTransform(1,1,1,0.6);
      
      public var fxctmap:ColorTransform = new ColorTransform(1.2,1.2,1.2,0.98);
      
      public var rect:Rectangle;
      
      public var blur:BlurFilter = new BlurFilter(2,2);
      
      public var maprect:Rectangle;
      
      private var _targets:Targets;
      
      public var sm:SimpleGlowFilter;
      
      public var hdrs:Boolean = false;
      
      public var sms:Boolean = true;
      
      public var stage:Stage;
      
      public var bmp:Bitmap;
      
      public var quadRenderer:Stage3DQuadRenderer;
      
      private var _bulletFadeFrames:int = 0;
      private var _partFadeFrames:int = 0;
      private var _minimapFilterTick:int = 0;
      
      public function RenderData()
      {
         super();
         this.renderwh = new Point(1920,1080);
      }
      
      public function disBmpds() : void
      {
         if(this.BMPD)
         {
            this.BMPD.dispose();
            this.BMPD = null;
         }
         if(this.CANVAS)
         {
            this.CANVAS.dispose();
            this.CANVAS = null;
         }
         if(this.BMPDFX)
         {
            this.BMPDFX.dispose();
            this.BMPDFX = null;
         }
         if(this.BMPDBLTFX)
         {
            this.BMPDBLTFX.dispose();
            this.BMPDBLTFX = null;
         }
         if(this.BMPDBG)
         {
            this.BMPDBG.dispose();
            this.BMPDBG = null;
         }
         if(this.MINIMAP)
         {
            this.MINIMAP.dispose();
            this.MINIMAP = null;
         }
         if(this.sm)
         {
            this.sm.dispose();
            this.sm = null;
         }
         if(this.bmp)
         {
            if(this.bmp.parent)
            {
               this.bmp.parent.removeChild(this.bmp);
            }
            this.bmp = null;
         }
      }
      
      public function init() : void
      {
         var _loc1_:DisplayObjectContainer = null;
         if(this.cam)
         {
            this.cam.rect.width = this.W;
            this.cam.rect.height = this.H;
         }
         if(Boolean(this.bmp) && Boolean(this.bmp.parent))
         {
            _loc1_ = this.bmp.parent;
         }
         if(this.BMPD)
         {
            this.disBmpds();
         }
         this.BMPD = new BitmapData(this.W,this.H,true,0);
         this.CANVAS = this.BMPD.clone();
         this.BMPDFX = new BitmapData(this.W / 2,this.H / 2,true,0);
         this.BMPDBLTFX = this.BMPDFX.clone();
         this.BMPDBG = this.BMPD.clone();
         if(!this.MINIMAP)
         {
            this.MINIMAP = new BitmapData(150,150,true,0);
         }
         this.MTX2X = new Matrix();
         this.MTX2X.scale(2,2);
         this.UISP = new Sprite();
         this.sm = new SimpleGlowFilter(this.CANVAS,0.45);
         this.maprect = this.MINIMAP.rect;
         this.rect = this.BMPD.rect;
         this.fxrect = this.BMPDBLTFX.rect;
         this.bmp = new Bitmap(this.CANVAS,PixelSnapping.AUTO,true);
         this.bmp.scaleX = 1920.0 / this.W;
         this.bmp.scaleY = 1080.0 / this.H;
         if(!this.sms)
         {
            this.bmp.transform.colorTransform = new ColorTransform(1.75,1.75,1.75);
         }
         if(ConfigAndStory.CONFIG[0] == 1)
         {
            this.sm.colorTransform.alphaMultiplier = 0.75;
            this.sm.blur.blurX = this.sm.blur.blurY = 4;
         }
         else if(ConfigAndStory.CONFIG[1] == 1)
         {
            this.sm.colorTransform.alphaMultiplier = 0.95;
            this.sm.blur.blurX = this.sm.blur.blurY = 8;
            this.sm.blur.quality = 2;
         }
         if(_loc1_)
         {
            _loc1_.addChildAt(this.bmp,0);
         }
         this.stage = InitData.STAGE;
         this._targets = InitData.TARGETS;
         try
         {
            if(this.stage && ("stage3Ds" in this.stage))
            {
               var _s3ds:* = this.stage["stage3Ds"];
               if(_s3ds && _s3ds.length > 0)
               {
                  var _s3d:* = _s3ds[0];
                  if(_s3d && _s3d.context3D)
                  {
                     this.initStage3D(_s3d.context3D);
                  }
                  else if(_s3d)
                  {
                     _s3d.addEventListener("context3DCreate", this.onContextCreated);
                     try {
                        _s3d.requestContext3D();
                     } catch(e1:Error) {}
                  }
               }
            }
         }
         catch(e2:Error) {}
      }

      private function onContextCreated(param1:Event) : void
      {
         var _s3d:* = param1.target;
         if(_s3d && _s3d.context3D)
         {
            this.initStage3D(_s3d.context3D);
         }
      }

      public function initStage3D(ctx:*) : void
      {
         if(!ctx) return;
         try
         {
            ctx.configureBackBuffer(this.W, this.H, 0, false);
            if(InitData.GDATA && InitData.GDATA.atlas)
            {
               InitData.GDATA.uploadStage3DAtlas(ctx);
            }
            this.quadRenderer = new Stage3DQuadRenderer(ctx, this.W, this.H);
            if(InitData.GDATA && InitData.GDATA.atlas)
            {
               this.quadRenderer.gpuTexture = InitData.GDATA.atlas.gpuTexture;
            }
         }
         catch(err:Error)
         {
            this.quadRenderer = null;
         }
      }
      
      public function setCameraDistance(param1:int) : void
      {
         if(param1 == 1)
         {
            this.W = 2496;
            this.H = 1404;
         }
         else if(param1 == 2)
         {
            this.W = 2880;
            this.H = 1620;
         }
         else
         {
            this.W = 1920;
            this.H = 1080;
         }
         this.renderwh.x = this.W;
         this.renderwh.y = this.H;
         if(this.cam)
         {
            this.cam.rect.width = this.W;
            this.cam.rect.height = this.H;
         }
         if(this.BMPD)
         {
            this.BMPD.dispose();
         }
         if(this.CANVAS)
         {
            this.CANVAS.dispose();
         }
         if(this.BMPDFX)
         {
            this.BMPDFX.dispose();
         }
         if(this.BMPDBLTFX)
         {
            this.BMPDBLTFX.dispose();
         }
         if(this.BMPDBG)
         {
            this.BMPDBG.dispose();
         }
         if(this.sm)
         {
            this.sm.dispose();
         }

         this.BMPD = new BitmapData(this.W,this.H,true,0);
         this.CANVAS = this.BMPD.clone();
         this.BMPDFX = new BitmapData(this.W / 2,this.H / 2,true,0);
         this.BMPDBLTFX = this.BMPDFX.clone();
         this.BMPDBG = this.BMPD.clone();
         this.rect = this.BMPD.rect;
         this.fxrect = this.BMPDBLTFX.rect;

         this.sm = new SimpleGlowFilter(this.CANVAS,0.45);
         if(ConfigAndStory.CONFIG[0] == 1)
         {
            this.sm.colorTransform.alphaMultiplier = 0.75;
            this.sm.blur.blurX = this.sm.blur.blurY = 4;
         }
         else if(ConfigAndStory.CONFIG[1] == 1)
         {
            this.sm.colorTransform.alphaMultiplier = 0.95;
            this.sm.blur.blurX = this.sm.blur.blurY = 8;
            this.sm.blur.quality = 2;
         }

         if(this.bmp)
         {
            this.bmp.bitmapData = this.CANVAS;
            this.bmp.scaleX = 1920.0 / this.W;
            this.bmp.scaleY = 1080.0 / this.H;
         }
      }
      
      public function makeCamBg(param1:int, param2:int) : void
      {
         this.cam = new Cam(new Rectangle(2000,2000,this.W,this.H),new Point(param1,param2),new Point(2500,2500));
         InitData.MAKER.cam = this.cam;
         this.drawbg = new BGMoveSimple(this.cam);
         this.drawbg.addBGAt(0.2,0.2,140,1,16777215,15,70,1);
         this.drawbg.addBGAt(0.4,0.4,100,2,16777215,20,60,3);
         this.drawbg.addBGAt(0.65,0.65,50,2,16777215,25,50,5);
         this.drawbg.addBGAt(1,1,50,2,16777215,15,40,7);
      }
      
      public function camCtrl() : void
      {
         this.cam.aim.x = this.stage.mouseX / this.bmp.scaleX;
         this.cam.aim.y = this.stage.mouseY / this.bmp.scaleY;
         if(this.cam.aim.x < 0)
         {
            this.cam.aim.x = 0;
         }
         else if(this.cam.aim.x > this.W)
         {
            this.cam.aim.x = this.W;
         }
         if(this.cam.aim.y < 0)
         {
            this.cam.aim.y = 0;
         }
         else if(this.cam.aim.y > this.H)
         {
            this.cam.aim.y = this.H;
         }
         this.cam.runCam();
      }
      
      public function render(param1:AIUnity) : void
      {
         this.camCtrl();
         var ctx:* = null;
         try
         {
            if(this.stage && ("stage3Ds" in this.stage))
            {
               var _s3ds:* = this.stage["stage3Ds"];
               if(_s3ds && _s3ds.length > 0 && _s3ds[0].context3D)
               {
                  ctx = _s3ds[0].context3D;
               }
            }
         }
         catch(eCtx:Error) {}

         if(ctx && this.quadRenderer && this.quadRenderer.isReady)
         {
            this.renderStage3D(ctx, param1);
         }
         else
         {
            if(ctx && !this.quadRenderer)
            {
               this.initStage3D(ctx);
            }
            this.renderSoftwareFallback(param1);
         }
      }

      public function renderStage3D(ctx:*, param1:AIUnity) : void
      {
         try
         {
            ctx.clear(0, 0, 0, 1);
            if(InitData.GDATA && InitData.GDATA.atlas && !this.quadRenderer.gpuTexture)
            {
               this.quadRenderer.gpuTexture = InitData.GDATA.atlas.gpuTexture;
            }
            this.quadRenderer.begin(this.W, this.H);

            var uvDisc:Object = InitData.GDATA ? InitData.GDATA.getStage3DUV("normal") : null;

            if(this.drawbg && this.drawbg.starbgs)
            {
               var numBGs:int = this.drawbg.starbgs.length;
               for(var bIdx:int = 0; bIdx < numBGs; bIdx++)
               {
                  var sb:* = this.drawbg.starbgs[bIdx];
                  if(sb && sb.stars)
                  {
                     var starsArr:Array = sb.stars;
                     var numStars:int = starsArr.length;
                     for(var sIdx:int = 0; sIdx < numStars; sIdx++)
                     {
                        var st:* = starsArr[sIdx];
                        if(st && st.rect)
                        {
                           var sAlpha:Number = ((st.color >> 24) & 0xFF) / 255.0;
                           var sR:Number = ((st.color >> 16) & 0xFF) / 255.0;
                           var sG:Number = ((st.color >> 8) & 0xFF) / 255.0;
                           var sB:Number = (st.color & 0xFF) / 255.0;
                           this.quadRenderer.renderQuad(st.rect.x, st.rect.y, st.rect.width * 2, st.rect.height * 2, 0, uvDisc, sR, sG, sB, sAlpha, false);
                        }
                     }
                  }
               }
            }

            if(this._targets)
            {
               if(this._targets.pba1)
               {
                  var pNum:int = this._targets.pba1.num;
                  var pArr:Array = this._targets.pba1.array;
                  for(var bi:int = 0; bi < pNum; bi++)
                  {
                     var b:* = pArr[bi];
                     if(b && b.move && b.move.point)
                     {
                        var buv:Object = InitData.GDATA ? InitData.GDATA.getStage3DUV(b.bulletData ? b.bulletData.mcName : "normal") : null;
                        var bx:Number = b.move.point.x - this.cam.rect.x;
                        var by:Number = b.move.point.y - this.cam.rect.y;
                        this.quadRenderer.renderQuad(bx, by, 16, 16, b.move.radFace, buv ? buv : uvDisc, 0, 0.8, 1, 1, true);
                     }
                  }
               }
               if(this._targets.eba1)
               {
                  var eNum:int = this._targets.eba1.num;
                  var eArr:Array = this._targets.eba1.array;
                  for(var ebi:int = 0; ebi < eNum; ebi++)
                  {
                     var eb:* = eArr[ebi];
                     if(eb && eb.move && eb.move.point)
                     {
                        var ebuv:Object = InitData.GDATA ? InitData.GDATA.getStage3DUV(eb.bulletData ? eb.bulletData.mcName : "normal") : null;
                        var ebx:Number = eb.move.point.x - this.cam.rect.x;
                        var eby:Number = eb.move.point.y - this.cam.rect.y;
                        this.quadRenderer.renderQuad(ebx, eby, 16, 16, eb.move.radFace, ebuv ? ebuv : uvDisc, 1, 0.3, 0.2, 1, true);
                     }
                  }
               }

               if(this._targets.pa1)
               {
                  var partNum:int = this._targets.pa1.num;
                  var partArr:Array = this._targets.pa1.array;
                  for(var pi:int = 0; pi < partNum; pi++)
                  {
                     var pt:* = partArr[pi];
                     if(pt && pt.point)
                     {
                        var ptx:Number = pt.point.x - this.cam.rect.x;
                        var pty:Number = pt.point.y - this.cam.rect.y;
                        var pAlpha:Number = pt.alpha > 0 ? pt.alpha / 255.0 : 0.8;
                        this.quadRenderer.renderQuad(ptx, pty, pt.w * 2, pt.w * 2, 0, uvDisc, 1, 0.7, 0.2, pAlpha, true);
                     }
                  }
               }
            }

            this.quadRenderer.flush();
            ctx.present();
         }
         catch(e:Error) {}

         if(this.MINIMAP && this._targets)
         {
            this.MINIMAP.lock();
            this.MINIMAP.colorTransform(this.maprect, this.fxctmap);
            this._targets.drawMiniMap(param1);
            if(++this._minimapFilterTick % 2 == 0)
            {
               this.MINIMAP.applyFilter(this.MINIMAP, this.maprect, this.point, this.blur);
            }
            this.MINIMAP.unlock();
         }
      }

      public function renderSoftwareFallback(param1:AIUnity) : void
      {
         var hasBullets:Boolean = this._targets != null && ((this._targets.pba1 != null && this._targets.pba1.num > 0) || (this._targets.eba1 != null && this._targets.eba1.num > 0));
         var hasParts:Boolean = this._targets != null && ((this._targets.pa1 != null && this._targets.pa1.num > 0) || (this._targets.deadParta1 != null && this._targets.deadParta1.num > 0));

         if(hasBullets)
         {
            this._bulletFadeFrames = 8;
         }
         if(hasParts)
         {
            this._partFadeFrames = 8;
         }

         if(this._bulletFadeFrames > 0)
         {
            this.BMPDBLTFX.colorTransform(this.fxrect,this.fxctblt);
         }
         this.BMPDFX.colorTransform(this.fxrect,this.fxct);
         this.drawbg.runBG();
         this._targets.renderPart();
         if(this._partFadeFrames > 0)
         {
            this.BMPD.colorTransform(this.rect,this.ct);
            this._partFadeFrames--;
         }
         this.BMPDFX.applyFilter(this.BMPDFX,this.fxrect,this.point,this.blur);
         if(this._bulletFadeFrames > 0)
         {
            this.BMPDBLTFX.applyFilter(this.BMPDBLTFX,this.fxrect,this.point,this.blur);
         }
         this._targets.runRender(param1);
         this._targets.renderBlt();
         this.CANVAS.lock();
         this.CANVAS.copyPixels(this.BMPDBG,this.rect,this.point);
         this.CANVAS.draw(this.BMPDFX,this.MTX2X);
         this.CANVAS.copyPixels(this.BMPD,this.rect,this.point,null,null,true);
         if(this._bulletFadeFrames > 0)
         {
            this.CANVAS.draw(this.BMPDBLTFX,this.MTX2X);
            this._bulletFadeFrames--;
         }
         if(this.sms)
         {
            this.sm.process();
         }
         this.CANVAS.unlock();
         this.MINIMAP.lock();
         this.MINIMAP.colorTransform(this.maprect,this.fxctmap);
         this._targets.drawMiniMap(param1);
         if(++this._minimapFilterTick % 2 == 0)
         {
            this.MINIMAP.applyFilter(this.MINIMAP,this.maprect,this.point,this.blur);
         }
         this.MINIMAP.unlock();
      }
   }
}

import flash.utils.ByteArray;

class Stage3DQuadRenderer
{
   public static const VERTEX_HEX:String = "a001000000a1001800000000000f03000000e400000000000000e4010000000000000000000f04010000e40000000000000000000000000000000001000f04020000e4000000000000000000000000";
   public static const FRAG_HEX:String = "a001000000a1012800000000000f02000000e40400000000000000050000100300000000000f03000000e402000000010000e404000000";

   public var context:*;
   public var program:*;
   public var vertexBuffer:*;
   public var indexBuffer:*;
   public var gpuTexture:*;
   public var isReady:Boolean = false;

   public var maxQuads:int = 2048;
   private var _quadCount:int = 0;
   private var _vertexData:Vector.<Number>;
   private var _orthoMatrix:Vector.<Number>;
   private var _currentBlendMode:String = "normal";

   public function Stage3DQuadRenderer(ctx:*, w:int, h:int)
   {
      super();
      this.context = ctx;
      this.initPipeline(w, h);
   }

   public static function hexToByteArray(hex:String) : ByteArray
   {
      var bytes:ByteArray = new ByteArray();
      for(var i:int = 0; i < hex.length; i += 2)
      {
         bytes.writeByte(parseInt(hex.substr(i, 2), 16));
      }
      bytes.position = 0;
      return bytes;
   }

   public function initPipeline(w:int, h:int) : void
   {
      if(!this.context) return;
      try
      {
         var vertBytes:ByteArray = hexToByteArray(VERTEX_HEX);
         var fragBytes:ByteArray = hexToByteArray(FRAG_HEX);
         this.program = this.context.createProgram();
         this.program.upload(vertBytes, fragBytes);

         this.vertexBuffer = this.context.createVertexBuffer(this.maxQuads * 4, 8);
         this.indexBuffer = this.context.createIndexBuffer(this.maxQuads * 6);

         var indices:Vector.<uint> = new Vector.<uint>(this.maxQuads * 6, true);
         for(var q:int = 0; q < this.maxQuads; q++)
         {
            var vIdx:int = q * 4;
            var iIdx:int = q * 6;
            indices[iIdx] = vIdx;
            indices[iIdx + 1] = vIdx + 1;
            indices[iIdx + 2] = vIdx + 2;
            indices[iIdx + 3] = vIdx;
            indices[iIdx + 4] = vIdx + 2;
            indices[iIdx + 5] = vIdx + 3;
         }
         this.indexBuffer.uploadFromVector(indices, 0, this.maxQuads * 6);

         this._vertexData = new Vector.<Number>(this.maxQuads * 32, false);
         this.updateProjection(w, h);
         this.isReady = true;
      }
      catch(e:Error)
      {
         this.isReady = false;
      }
   }

   public function updateProjection(w:int, h:int) : void
   {
      this._orthoMatrix = new <Number>[
         2.0 / Number(w), 0, 0, 0,
         0, -2.0 / Number(h), 0, 0,
         0, 0, 1, 0,
         -1, 1, 0, 1
      ];
   }

   public function begin(w:int, h:int) : void
   {
      this._quadCount = 0;
      this._currentBlendMode = "normal";
   }

   public function renderQuad(x:Number, y:Number, w:Number, h:Number, rotation:Number, uv:Object, r:Number = 1, g:Number = 1, b:Number = 1, a:Number = 1, additive:Boolean = false) : void
   {
      if(additive && this._currentBlendMode != "additive")
      {
         if(this._quadCount > 0) this.flush();
         this._currentBlendMode = "additive";
      }
      else if(!additive && this._currentBlendMode != "normal")
      {
         if(this._quadCount > 0) this.flush();
         this._currentBlendMode = "normal";
      }

      if(this._quadCount >= this.maxQuads)
      {
         this.flush();
      }

      var u0:Number = uv ? Number(uv.u0) : 0.0;
      var v0:Number = uv ? Number(uv.v0) : 0.0;
      var u1:Number = uv ? Number(uv.u1) : 1.0;
      var v1:Number = uv ? Number(uv.v1) : 1.0;

      var hw:Number = w * 0.5;
      var hh:Number = h * 0.5;
      var cos:Number = Math.cos(rotation);
      var sin:Number = Math.sin(rotation);

      var x0:Number = x + (-hw * cos - -hh * sin);
      var y0:Number = y + (-hw * sin + -hh * cos);
      var x1:Number = x + (hw * cos - -hh * sin);
      var y1:Number = y + (hw * sin + -hh * cos);
      var x2:Number = x + (hw * cos - hh * sin);
      var y2:Number = y + (hw * sin + hh * cos);
      var x3:Number = x + (-hw * cos - hh * sin);
      var y3:Number = y + (-hw * sin + hh * cos);

      var idx:int = this._quadCount * 32;
      this._vertexData[idx] = x0; this._vertexData[idx+1] = y0; this._vertexData[idx+2] = u0; this._vertexData[idx+3] = v0;
      this._vertexData[idx+4] = r; this._vertexData[idx+5] = g; this._vertexData[idx+6] = b; this._vertexData[idx+7] = a;

      this._vertexData[idx+8] = x1; this._vertexData[idx+9] = y1; this._vertexData[idx+10] = u1; this._vertexData[idx+11] = v0;
      this._vertexData[idx+12] = r; this._vertexData[idx+13] = g; this._vertexData[idx+14] = b; this._vertexData[idx+15] = a;

      this._vertexData[idx+16] = x2; this._vertexData[idx+17] = y2; this._vertexData[idx+18] = u1; this._vertexData[idx+19] = v1;
      this._vertexData[idx+20] = r; this._vertexData[idx+21] = g; this._vertexData[idx+22] = b; this._vertexData[idx+23] = a;

      this._vertexData[idx+24] = x3; this._vertexData[idx+25] = y3; this._vertexData[idx+26] = u0; this._vertexData[idx+27] = v1;
      this._vertexData[idx+28] = r; this._vertexData[idx+29] = g; this._vertexData[idx+30] = b; this._vertexData[idx+31] = a;

      this._quadCount++;
   }

   public function flush() : void
   {
      if(this._quadCount == 0 || !this.context || !this.program || !this.vertexBuffer) return;
      try
      {
         this.vertexBuffer.uploadFromVector(this._vertexData, 0, this._quadCount * 4);
         this.context.setProgram(this.program);
         if(this.gpuTexture)
         {
            this.context.setTextureAt(0, this.gpuTexture);
         }
         this.context.setVertexBufferAt(0, this.vertexBuffer, 0, "float2");
         this.context.setVertexBufferAt(1, this.vertexBuffer, 2, "float2");
         this.context.setVertexBufferAt(2, this.vertexBuffer, 4, "float4");
         this.context.setProgramConstantsFromVector("vertex", 0, this._orthoMatrix, 4);

         if(this._currentBlendMode == "additive")
         {
            this.context.setBlendFactors("one", "one");
         }
         else
         {
            this.context.setBlendFactors("sourceAlpha", "oneMinusSourceAlpha");
         }

         this.context.drawTriangles(this.indexBuffer, 0, this._quadCount * 2);
      }
      catch(e:Error) {}
      this._quadCount = 0;
   }
}

