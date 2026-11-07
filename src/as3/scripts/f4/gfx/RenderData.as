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
      
      public var W:int = 640;
      
      public var H:int = 480;
      
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
      
      private var _bulletFadeFrames:int = 0;
      private var _partFadeFrames:int = 0;
      private var _minimapFilterTick:int = 0;
      
      public function RenderData()
      {
         super();
         this.renderwh = new Point(640,480);
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
         this.bmp.scaleX = 640 / this.W;
         this.bmp.scaleY = 480 / this.H;
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
      }
      
      public function setCameraDistance(param1:int) : void
      {
         if(param1 == 1)
         {
            this.W = 832;
            this.H = 624;
         }
         else if(param1 == 2)
         {
            this.W = 960;
            this.H = 720;
         }
         else
         {
            this.W = 640;
            this.H = 480;
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
            this.bmp.scaleX = 640.0 / this.W;
            this.bmp.scaleY = 480.0 / this.H;
         }
      }
      
      public function makeCamBg(param1:int, param2:int) : void
      {
         this.cam = new Cam(new Rectangle(2000,2000,this.W,this.H),new Point(param1,param2),new Point(2500,2500));
         InitData.MAKER.cam = this.cam;
         this.drawbg = new BGMoveSimple(this.cam);
         this.drawbg.addBGAt(0.2,0.2,70,1,16777215,15,70,1);
         this.drawbg.addBGAt(0.4,0.4,50,2,16777215,20,60,3);
         this.drawbg.addBGAt(0.65,0.65,25,2,16777215,25,50,5);
         this.drawbg.addBGAt(1,1,25,2,16777215,15,40,7);
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

