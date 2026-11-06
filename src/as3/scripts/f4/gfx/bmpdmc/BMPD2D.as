package f4.gfx.bmpdmc
{
   import f4.data.InitData;
   import f4.gfx.RenderData;
   import flash.display.BitmapData;
   import flash.geom.ColorTransform;
   import flash.geom.Matrix;
   import flash.geom.Point;
   import fullshit.tool.MathTool;
   import fullshit.tool.StaticShit;
   
   public class BMPD2D
   {
      
      public var drawPoint:Point;
      
      public var renderData:RenderData;
      
      public var bmpdname:String;
      
      public var bmpd:BMPDMC;
      
      private var _math:MathTool;
      
      public var mtx:Matrix;
      
      public var ct:ColorTransform;
      
      public var ct2:ColorTransform;
      
      private var _pidface:Number;
      
      private var _face:int;
      
      public var bg:Boolean;
      
      public function BMPD2D(param1:Boolean, param2:BMPDMC)
      {
         super();
         this.renderData = InitData.RENDERDATA;
         this._pidface = InitData.RENDERDATA.PIDFACE;
         this._face = InitData.RENDERDATA.FACE;
         this._math = StaticShit.MATHTOOL;
         this.drawPoint = new Point();
         this.init(param1,param2);
         this.mtx = new Matrix();
         this.ct = new ColorTransform(0.1,0.1,0.1,1,255,25,25);
         this.ct2 = new ColorTransform(0.1,0.1,0.1,1,100,25,25);
      }
      
      public function changeBmpdRad(param1:Number) : void
      {
         param1 = this._math.formatRad(param1);
         var _loc2_:int = Math.round(param1 / this._pidface);
         if(_loc2_ < 0)
         {
            _loc2_ += this._face;
         }
         this.bmpd.currentFrame = _loc2_;
         this.bmpd.moveRect();
      }
      
      public function init(param1:Boolean, param2:BMPDMC) : void
      {
         this.bmpd = param2;
         this.changeBmpdRad(0);
         this.bg = param1;
      }
      
      public function makeDrawPoint(param1:Number, param2:Number) : void
      {
         this.drawPoint.x = param1 - this.bmpd.rectSmall.width / 2;
         this.drawPoint.y = param2 - this.bmpd.rectSmall.height / 2;
      }
      
      public function render(param1:int) : void
      {
         var _loc2_:BitmapData = null;
         if(this.bg)
         {
            _loc2_ = this.renderData.BMPDBG;
         }
         else
         {
            _loc2_ = this.renderData.BMPD;
         }
         if(param1 == 0)
         {
            _loc2_.copyPixels(this.bmpd.bmpd,this.bmpd.rectSmall,this.drawPoint,null,null,true);
         }
         else if(param1 == 1)
         {
            this.mtx.tx = this.drawPoint.x - this.bmpd.rectSmall.x;
            this.mtx.ty = this.drawPoint.y - this.bmpd.rectSmall.y;
            this.bmpd.rectSmall.x = this.drawPoint.x;
            this.bmpd.rectSmall.y = this.drawPoint.y;
            _loc2_.draw(this.bmpd.bmpd,this.mtx,this.ct,null,this.bmpd.rectSmall);
         }
         else if(param1 == 2)
         {
            this.mtx.tx = this.drawPoint.x - this.bmpd.rectSmall.x;
            this.mtx.ty = this.drawPoint.y - this.bmpd.rectSmall.y;
            this.bmpd.rectSmall.x = this.drawPoint.x;
            this.bmpd.rectSmall.y = this.drawPoint.y;
            _loc2_.draw(this.bmpd.bmpd,this.mtx,this.ct2,null,this.bmpd.rectSmall);
         }
      }
   }
}

