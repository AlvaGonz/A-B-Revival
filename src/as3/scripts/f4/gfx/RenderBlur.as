package f4.gfx
{
   import flash.display.BitmapData;
   import flash.filters.BlurFilter;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class RenderBlur
   {
      
      public var blur:BlurFilter;
      
      public var blurNum:Point;
      
      public var blurNumMin:Point;
      
      public var blurNumScale:Point;
      
      public function RenderBlur()
      {
         super();
         this.blur = new BlurFilter(4,4,2);
         this.blurNum = new Point();
         this.blurNumMin = new Point(1,1);
         this.blurNumScale = new Point(0.95,0.95);
      }
      
      public function runBlur(param1:BitmapData, param2:Rectangle, param3:Point) : void
      {
         this.blurNum.x *= this.blurNumScale.x;
         this.blurNum.y *= this.blurNumScale.y;
         if(this.blurNum.x < this.blurNumMin.x)
         {
            this.blurNum.x = this.blurNumMin.x;
         }
         if(this.blurNum.y < this.blurNumMin.y)
         {
            this.blurNum.y = this.blurNumMin.y;
         }
         if(this.blurNum.x > 1 || this.blurNum.y > 1)
         {
            this.blur.blurX = this.blurNum.x;
            this.blur.blurY = this.blurNum.y;
            param1.applyFilter(param1,param2,param3,this.blur);
         }
      }
   }
}

