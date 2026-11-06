package f4.data.sound32
{
   import f4.data.InitData;
   import f4.gfx.RenderData;
   import flash.geom.Point;
   import fullshit.ds.AbsOne;
   
   public class SoundRun1 extends AbsOne
   {
      
      public var renderData:RenderData;
      
      public var point:Point;
      
      public var pan:Number;
      
      public var vol:Number;
      
      public var volOutMut:Number;
      
      public function SoundRun1(param1:Point)
      {
         super();
         this.volOutMut = 1;
         this.pan = 0;
         this.vol = 0;
         this.renderData = InitData.RENDERDATA;
         this.point = param1;
         this.runA1();
      }
      
      override public function runA1() : void
      {
         var _loc1_:Number = this.renderData.cam.rect.x + this.renderData.cam.rect.width / 2;
         var _loc2_:Number = this.renderData.cam.rect.y + this.renderData.cam.rect.height / 2;
         this.pan = (this.point.x - _loc1_) / 320;
         if(this.pan > 1)
         {
            this.pan = 1;
         }
         else if(this.pan < -1)
         {
            this.pan = -1;
         }
         var _loc3_:Number = Math.sqrt((this.point.x - _loc1_) * (this.point.x - _loc1_) + (this.point.y - _loc2_) * (this.point.y - _loc2_));
         this.vol = (400 - _loc3_) / 400 * this.volOutMut;
         if(this.vol < 0)
         {
            this.vol = 0;
         }
      }
   }
}

