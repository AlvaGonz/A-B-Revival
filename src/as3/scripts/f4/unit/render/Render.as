package f4.unit.render
{
   import f4.data.InitData;
   import f4.gfx.RenderData;
   import f4.unit.move.MoveAs;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.tool.BresenhamLine;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class Render
   {
      
      public var move:MoveAs;
      
      public var cam:Cam;
      
      public var renderData:RenderData;
      
      public var ckRect:Rectangle;
      
      public var rect2:Rect2 = StaticShit.RECT2;
      
      public var bline:BresenhamLine = StaticShit.BLINE;
      
      public var h:Number;
      
      public var color:uint;
      
      public var colorHead:uint;
      
      private var _drawRect:Rectangle;
      
      public var inRender:Boolean;
      
      public var road:Boolean;
      
      public var head:Boolean;
      
      public function Render(param1:MoveAs)
      {
         super();
         this.move = param1;
         this.ckRect = new Rectangle(0,0,0,0);
         this.renderData = InitData.RENDERDATA;
         this._drawRect = new Rectangle();
         this.h = 0;
         this.color = 16777215;
         this.road = true;
      }
      
      public function changeColor(param1:Number) : void
      {
         this.h = param1;
         StaticShit.RGB.H = this.h + Math.random() * 60 - 30;
         StaticShit.RGB.HSV2RGB();
         this.color = StaticShit.RGB.rgb;
         StaticShit.RGB.H = this.h + 60 + Math.random() * 20 - 10;
         StaticShit.RGB.HSV2RGB();
         this.colorHead = StaticShit.RGB.rgb;
      }
      
      public function updateRect() : void
      {
         if(this.move.point.x < this.move.lastPoint.x)
         {
            this.ckRect.x = this.move.point.x;
            this.ckRect.width = this.move.lastPoint.x - this.move.point.x;
         }
         else
         {
            this.ckRect.x = this.move.lastPoint.x;
            this.ckRect.width = this.move.point.x - this.move.lastPoint.x;
         }
         if(this.move.point.y < this.move.lastPoint.y)
         {
            this.ckRect.y = this.move.point.y;
            this.ckRect.height = this.move.lastPoint.y - this.move.point.y;
         }
         else
         {
            this.ckRect.y = this.move.lastPoint.y;
            this.ckRect.height = this.move.point.y - this.move.lastPoint.y;
         }
      }
      
      public function render(param1:Number) : void
      {
         var _loc2_:Number = NaN;
         if(this.road)
         {
            _loc2_ = this.move.hitRect.width / 2;
            if(_loc2_ > 1)
            {
               this.bline.lineRect32((this.move.lastPoint.x - this.cam.last.x) / 2,(this.move.point.x - this.cam.rect.x) / 2,(this.move.lastPoint.y - this.cam.last.y) / 2,(this.move.point.y - this.cam.rect.y) / 2,_loc2_,this.renderData.BMPDBLTFX,this.color);
            }
            else
            {
               this.bline.line32((this.move.lastPoint.x - this.cam.last.x) / 2,(this.move.point.x - this.cam.rect.x) / 2,(this.move.lastPoint.y - this.cam.last.y) / 2,(this.move.point.y - this.cam.rect.y) / 2,this.renderData.BMPDBLTFX,this.color);
            }
         }
         else
         {
            this._drawRect.x = (this.move.point.x - this.move.hitRect.width / 2 - this.cam.rect.x) / 2;
            this._drawRect.y = (this.move.point.y - this.move.hitRect.height / 2 - this.cam.rect.y) / 2;
            this._drawRect.width = this.move.hitRect.width / 2;
            this._drawRect.height = this.move.hitRect.height / 2;
            this.renderData.BMPDBLTFX.fillRect(this._drawRect,255 << 24 | this.color);
         }
      }
      
      public function runRender(param1:Number) : void
      {
         this.inRender = false;
         this.updateRect();
         var _loc2_:Number = this.move.point.x - this.cam.rect.x - this.cam.rect.width / 2;
         var _loc3_:Number = this.move.point.y - this.cam.rect.y - this.cam.rect.height / 2;
         if(_loc2_ < 0)
         {
            _loc2_ = -_loc2_;
         }
         if(_loc3_ < 0)
         {
            _loc3_ = -_loc3_;
         }
         if(_loc2_ < 1000 && _loc3_ < 1000)
         {
            this.inRender = true;
         }
         if(this.rect2.rectIntersects(this.ckRect,this.cam.rect))
         {
            this.render(param1);
         }
      }
      
      public function drawLife(param1:Point) : void
      {
         var _loc2_:Number = param1.x / param1.y;
         var _loc3_:Number = this.move.hitRect.x - this.cam.rect.x;
         var _loc4_:Number = this.move.hitRect.y - this.cam.rect.y;
         this._drawRect.x = _loc3_ / 2;
         this._drawRect.y = _loc4_ / 2;
         this._drawRect.width = this.move.hitRect.width / 2;
         this._drawRect.height = 1;
         this.renderData.BMPDBLTFX.fillRect(this._drawRect,1435011208);
         this._drawRect.width *= _loc2_;
         this._drawRect.height = 1;
         if(_loc2_ > 0.75)
         {
            this.renderData.BMPDBLTFX.fillRect(this._drawRect,1426128640);
         }
         else if(_loc2_ > 0.5)
         {
            this.renderData.BMPDBLTFX.fillRect(this._drawRect,1442840320);
         }
         else
         {
            this.renderData.BMPDBLTFX.fillRect(this._drawRect,1442775040);
         }
      }
   }
}

