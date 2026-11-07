package f4.unit.render
{
   import f4.data.InitData;
   import f4.gfx.RenderData;
   import f4data.sdata.ConfigAndStory;
   
   public class BGMoveSimple
   {
      
      public var cam:Cam;
      
      public var renderData:RenderData;
      
      public var starbgs:Array;
      
      public var cpx:BGMoves;
      
      public function BGMoveSimple(param1:Cam)
      {
         super();
         this.cam = param1;
         this.renderData = InitData.RENDERDATA;
         this.starbgs = [];
         this.cpx = new BGMoves(param1,this.starbgs);
      }
      
      public function addBGAt(param1:Number, param2:Number, param3:int, param4:Number, param5:uint, param6:uint, param7:uint, param8:int) : void
      {
         var _bw:Number = (this.renderData ? this.renderData.W : 1920) + 100;
         var _bh:Number = (this.renderData ? this.renderData.H : 1080) + 100;
         var _loc9_:StarBGUnitSimple = new StarBGUnitSimple(100 / param1,100 / param2,_bw / param1,_bh / param2,param1,param2);
         _loc9_.initStars(param3,param4,param5,param6,param7);
         this.starbgs[param8] = _loc9_;
      }
      
      public function runBG() : void
      {
         this.renderData.BMPDBG.fillRect(this.renderData.rect,0);
         var _loc1_:int = int(this.starbgs.length);
         var _loc2_:int = -1;
         var _loc3_:Boolean = ConfigAndStory.CONFIG[5] == 1;
         while(++_loc2_ < _loc1_)
         {
            if(this.starbgs[_loc2_])
            {
               if(this.starbgs[_loc2_] is StarBGUnit)
               {
                  if(_loc3_)
                  {
                     (this.starbgs[_loc2_] as StarBGUnit).renderFrom(this.cam.rect.x,this.cam.rect.y,this.cam.rect.width,this.cam.rect.height);
                  }
               }
               else
               {
                  (this.starbgs[_loc2_] as StarBGUnitSimple).renderFrom(this.renderData.BMPDBG,this.cam.rect.x - this.cam.last.x,this.cam.rect.y - this.cam.last.y);
               }
            }
         }
      }
   }
}

