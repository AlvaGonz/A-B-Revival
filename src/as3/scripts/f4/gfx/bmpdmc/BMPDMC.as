package f4.gfx.bmpdmc
{
   import flash.display.BitmapData;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class BMPDMC
   {
      
      private var _sqrtFrame:int;
      
      public var rectSmall:Rectangle;
      
      public var bmpd:BitmapData;
      
      public var frames:int;
      
      public var currentFrame:int;
      
      public var copyPoint:Point;
      
      public var bigBmpdName:String;
      
      public function BMPDMC(param1:int, param2:Rectangle, param3:BitmapData, param4:String)
      {
         super();
         this.init(param1,param2,param3,param4);
         this.copyPoint = new Point();
      }
      
      public function init(param1:int, param2:Rectangle, param3:BitmapData, param4:String) : void
      {
         this.rectSmall = param2;
         this.bigBmpdName = param4;
         this.frames = param1;
         this.currentFrame = 0;
         this.bmpd = param3;
         this._sqrtFrame = Math.ceil(Math.sqrt(this.frames));
      }
      
      public function moveRect() : void
      {
         var _loc1_:int = this.currentFrame / this._sqrtFrame;
         this.rectSmall.y = _loc1_ * this.rectSmall.height;
         this.rectSmall.x = (this.currentFrame - _loc1_ * this._sqrtFrame) * this.rectSmall.width;
      }
      
      public function nextFrame() : void
      {
         if(++this.currentFrame >= this.frames)
         {
            this.currentFrame = 0;
         }
      }
   }
}

