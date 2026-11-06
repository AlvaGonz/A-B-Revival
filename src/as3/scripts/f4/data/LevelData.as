package f4.data
{
   import f4.gfx.GData;
   import f4.gfx.RenderData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.MovieClip;
   import flash.utils.getQualifiedClassName;
   import fullshit.tool.Attach;
   
   public class LevelData
   {
      
      public var gdata:GData;
      
      public var renderData:RenderData;
      
      public var afa:Array;
      
      public var efa:Array;
      
      public var bga:Array;
      
      public function LevelData()
      {
         super();
         this.gdata = InitData.GDATA;
         this.renderData = InitData.RENDERDATA;
         this.afa = [];
         this.efa = [];
         this.bga = [];
      }
      
      public function initLevel(param1:int, param2:int, param3:String) : void
      {
         var _loc4_:int = 0;
         this.renderData.makeCamBg(param1,param2);
         if(ConfigAndStory.STORY == 99)
         {
            _loc4_ = Math.random() * 4 + 1;
            this.initBG("BG" + _loc4_);
         }
         else
         {
            this.initBG(param3);
         }
         this.gdata.initListBG(this.bga,this.renderData.drawbg);
         this.gdata.initListFly(this.afa);
         this.gdata.initListFly(this.efa);
      }
      
      public function initBG(param1:String) : void
      {
         var _loc2_:String = null;
         var _loc6_:MovieClip = null;
         var _loc7_:int = 0;
         var _loc8_:int = 0;
         var _loc9_:int = 0;
         var _loc10_:Number = NaN;
         var _loc11_:Number = NaN;
         var _loc12_:Number = NaN;
         var _loc13_:Number = NaN;
         var _loc15_:int = 0;
         var _loc16_:MovieClip = null;
         var _loc17_:String = null;
         var _loc18_:Array = null;
         this.bga.length = 0;
         var _loc3_:MovieClip = Attach.attach(param1) as MovieClip;
         var _loc4_:int = -1;
         var _loc5_:int = _loc3_.numChildren;
         var _loc14_:int = -1;
         while(++_loc4_ < _loc5_)
         {
            _loc6_ = _loc3_.getChildAt(_loc4_) as MovieClip;
            _loc2_ = _loc6_.name.substr(3);
            _loc7_ = parseInt(_loc2_.substring(0,_loc2_.indexOf("_")));
            _loc2_ = _loc2_.substring(_loc2_.indexOf("_") + 1);
            _loc8_ = parseInt(_loc2_.substring(0,_loc2_.indexOf("_")));
            _loc2_ = _loc2_.substring(_loc2_.indexOf("_") + 1);
            _loc9_ = parseInt(_loc2_);
            _loc10_ = this.renderData.cam.wh.x / _loc6_.getChildByName("RECT").width;
            _loc11_ = this.renderData.cam.wh.y / _loc6_.getChildByName("RECT").height;
            _loc12_ = this.renderData.cam.wh.x / _loc8_;
            _loc13_ = this.renderData.cam.wh.y / _loc9_;
            _loc18_ = [_loc7_,_loc12_,_loc13_,_loc10_,_loc11_];
            _loc14_ = -1;
            _loc15_ = _loc6_.numChildren;
            while(++_loc14_ < _loc15_)
            {
               _loc16_ = _loc6_.getChildAt(_loc14_) as MovieClip;
               if(_loc16_.name != "RECT")
               {
                  _loc17_ = getQualifiedClassName(_loc16_);
                  _loc18_.push([_loc17_,_loc16_.x,_loc16_.y]);
               }
            }
            this.bga.push(_loc18_);
         }
      }
   }
}

