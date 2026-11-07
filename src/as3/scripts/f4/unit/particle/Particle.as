package f4.unit.particle
{
   import f4.data.InitData;
   import f4.gfx.RenderData;
   import f4.unit.move.MoveAs;
   import f4.unit.render.Cam;
   import flash.display.BitmapData;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.ds.AbsOne;
   import fullshit.tool.BresenhamLine;
   import fullshit.tool.RGBHSLHSV;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class Particle extends AbsOne
   {
      
      public var point:Point;
      
      public var spd:Point;
      
      public var life:int;
      
      public var autof:Number;
      
      protected var _rect2:Rect2;
      
      private var _bline:BresenhamLine;
      
      private var _hsv:RGBHSLHSV;
      
      public var color:uint;
      
      public var rect:Rectangle;
      
      public var cam:Cam;
      
      public var renderData:RenderData;
      
      public var w:Number;
      
      public var alpha:int = 255;
      
      public var flash:Boolean;
      
      public var flashRect:Rectangle;
      
      public var lastRect:Rectangle;
      
      public var layer:int;
      
      public var useLine:Boolean = true;
      
      public function Particle(param1:Cam, param2:Number, param3:Number, param4:Number, param5:Number, param6:Number, param7:Number, param8:Number, param9:int)
      {
         super();
         this.point = new Point(param2,param3);
         this.spd = new Point(Math.cos(param4) * param5,Math.sin(param4) * param5);
         this._rect2 = StaticShit.RECT2;
         this._bline = StaticShit.BLINE;
         this._hsv = StaticShit.RGB;
         this.life = Math.round(param9 * (1.0 / MoveAs.SIM_SCALE));
         this.autof = param8;
         this.rect = new Rectangle(0,0,this.w,this.w);
         this.cam = param1;
         this.renderData = InitData.RENDERDATA;
         this._hsv.H = param7 + Math.random() * 30 - 15;
         this._hsv.hsvS = 0.8;
         this._hsv.hsvV = 1;
         this._hsv.HSV2RGB();
         this.color = this._hsv.rgb;
         this.w = param6;
         this.point.x -= this.cam.last.x - this.cam.rect.x;
         this.point.y -= this.cam.last.y - this.cam.rect.y;
         this.flash = true;
         this.flashRect = new Rectangle(0,0,2,2);
         this.lastRect = new Rectangle(0,0,this.cam.rect.width,this.cam.rect.height);
         if(InitData.TARGETS && InitData.TARGETS.pa1 && InitData.TARGETS.pa1.num > 400 && Math.random() < 0.5)
         {
            isDeadA1 = true;
         }
      }
      
      override public function startRemoveA1() : void
      {
         this.cam = null;
         this.renderData = null;
         this.point = null;
         this.spd = null;
         this.rect = null;
         this.flashRect = null;
         this.lastRect = null;
         this._rect2 = null;
         this._bline = null;
         this._hsv = null;
      }
      
      public function colorFromBmpd(param1:BitmapData) : void
      {
         var _loc2_:int = 0;
         this.color = 0;
         while(this.color == 0)
         {
            this.color = param1.getPixel32(int(Math.random() * param1.width),int(Math.random() * param1.height));
            if(++_loc2_ > 100)
            {
               break;
            }
         }
      }
      
      override public function runA1() : void
      {
         var sx:Number = this.spd.x * MoveAs.SIM_SCALE;
         var sy:Number = this.spd.y * MoveAs.SIM_SCALE;
         this.point.x += sx;
         this.point.y += sy;
         if(sx < 0)
         {
            this.rect.x = this.point.x;
            this.rect.width = -sx;
         }
         else
         {
            this.rect.x = this.point.x - sx;
            this.rect.width = sx;
         }
         if(sy < 0)
         {
            this.rect.y = this.point.y;
            this.rect.height = -sy;
         }
         else
         {
            this.rect.y = this.point.y - sy;
            this.rect.height = sy;
         }
         if(this.autof != 1)
         {
            var decay:Number = Math.pow(this.autof, MoveAs.SIM_SCALE);
            this.spd.x *= decay;
            this.spd.y *= decay;
         }
         if(this.life-- <= 0)
         {
            this.alpha -= 20;
         }
         if(this.alpha <= 0 || (this.point.x < -300 || this.point.x > this.cam.wh.x + 300 || this.point.y < -300 || this.point.y > this.cam.wh.y + 300))
         {
            isDeadA1 = true;
         }
      }
      
      public function render() : void
      {
         var _loc1_:BitmapData = null;
         this.lastRect.x = this.cam.last.x;
         this.lastRect.y = this.cam.last.y;
         if(this.layer == 0)
         {
            _loc1_ = this.renderData.BMPDFX;
         }
         else
         {
            _loc1_ = this.renderData.BMPDBLTFX;
         }
         if(!isDeadA1 && this._rect2.rectIntersects(this.lastRect,this.rect))
         {
            if(this.useLine)
            {
               if(this.w < 2)
               {
                  this._bline.line32N24((this.point.x - this.spd.x - this.cam.last.x) / 2,(this.point.x - this.cam.rect.x) / 2,(this.point.y - this.spd.y - this.cam.last.y) / 2,(this.point.y - this.cam.rect.y) / 2,_loc1_,this.alpha << 24 | this.color);
               }
               else
               {
                  this._bline.lineRect((this.point.x - this.spd.x - this.cam.last.x) / 2,(this.point.x - this.cam.rect.x) / 2,(this.point.y - this.spd.y - this.cam.last.y) / 2,(this.point.y - this.cam.rect.y) / 2,this.w,_loc1_,this.alpha << 24 | this.color);
               }
            }
            else
            {
               this.flashRect.x = (this.point.x - this.cam.rect.x) / 2;
               this.flashRect.y = (this.point.y - this.cam.rect.y) / 2;
               _loc1_.fillRect(this.flashRect,this.alpha << 24 | this.color);
            }
            if(this.flash)
            {
               if(Math.random() < 0.25)
               {
                  this.flashRect.x = (this.point.x - this.cam.rect.x) / 2 + Math.random() * 4 - 2;
                  this.flashRect.y = (this.point.y - this.cam.rect.y) / 2 + Math.random() * 4 - 2;
                  _loc1_.fillRect(this.flashRect,this.alpha << 24 | 0xFFFFFF);
               }
            }
         }
      }
   }
}

