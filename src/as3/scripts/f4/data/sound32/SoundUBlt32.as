package f4.data.sound32
{
   import f4.data.InitData;
   import f4.gfx.RenderData;
   import flash.geom.Point;
   import flash.media.Sound;
   
   public class SoundUBlt32 extends SoundUnit32
   {
      
      public var timer:uint = 0;
      
      public var renderData:RenderData = InitData.RENDERDATA;
      
      public var point:Point;
      
      public var volMut:Number = 1;
      
      public function SoundUBlt32(param1:Sound, param2:int, param3:Point)
      {
         this.point = param3;
         super(param1,param2,false);
      }
      
      override public function runA1() : void
      {
         var _loc1_:Number = this.renderData.cam.rect.x + this.renderData.cam.rect.width / 2;
         var _loc2_:Number = this.renderData.cam.rect.y + this.renderData.cam.rect.height / 2;
         pan = (this.point.x - _loc1_) / 320;
         if(pan > 1)
         {
            pan = 1;
         }
         else if(pan < -1)
         {
            pan = -1;
         }
         var _loc3_:Number = Math.sqrt((this.point.x - _loc1_) * (this.point.x - _loc1_) + (this.point.y - _loc2_) * (this.point.y - _loc2_));
         vol = (400 - _loc3_) / 400 * this.volMut;
         if(vol < 0)
         {
            vol = 0;
         }
         else if(vol > 1.4)
         {
            vol = 1.4;
         }
         changeVolPan();
         ++this.timer;
      }
      
      public function ckVol() : void
      {
         var _loc1_:Number = this.renderData.cam.rect.x + this.renderData.cam.rect.width / 2;
         var _loc2_:Number = this.renderData.cam.rect.y + this.renderData.cam.rect.height / 2;
         var _loc3_:Number = Math.sqrt((this.point.x - _loc1_) * (this.point.x - _loc1_) + (this.point.y - _loc2_) * (this.point.y - _loc2_));
         pan = (this.point.x - _loc1_) / 320;
         if(pan > 1)
         {
            pan = 1;
         }
         else if(pan < -1)
         {
            pan = -1;
         }
         vol = (400 - _loc3_) / 400;
         if(vol < 0)
         {
            vol = 0;
         }
      }
   }
}

