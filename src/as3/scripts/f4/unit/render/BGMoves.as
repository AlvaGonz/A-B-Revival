package f4.unit.render
{
   import f4.gfx.bmpdmc.BMPD2D;
   
   public class BGMoves
   {
      
      public var cam:Cam;
      
      public var starbgs:Array;
      
      public function BGMoves(param1:Cam, param2:Array)
      {
         super();
         this.cam = param1;
         this.starbgs = param2;
      }
      
      public function addBGAt(param1:Number, param2:Number, param3:int) : void
      {
         var _loc4_:StarBGUnit = new StarBGUnit(this.cam.wh.x / param1,this.cam.wh.y / param2,param1,param2,this.cam.rect.width * 2 / param1,this.cam.rect.height * 2 / param1);
         this.starbgs[param3] = _loc4_;
      }
      
      public function addBmpd2dAt(param1:BMPD2D, param2:Number, param3:Number, param4:Number, param5:Number, param6:int) : void
      {
         if(Boolean(this.starbgs[param6]) && this.starbgs[param6] is StarBGUnit)
         {
            (this.starbgs[param6] as StarBGUnit).addBmpd2d(param1,param2,param3,param4,param5);
         }
      }
      
      public function runBG() : void
      {
         var _loc1_:StarBGUnit = null;
         for each(_loc1_ in this.starbgs)
         {
            _loc1_.renderFrom(this.cam.rect.x,this.cam.rect.y,this.cam.rect.width,this.cam.rect.height);
         }
      }
   }
}

