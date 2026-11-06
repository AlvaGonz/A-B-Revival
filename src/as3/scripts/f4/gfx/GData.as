package f4.gfx
{
   import f4.gfx.bmpdmc.BMPDMCData2;
   import f4.gfx.bmpdmc.MC2BMPD;
   import f4.unit.render.BGMoveSimple;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import flash.display.MovieClip;
   import flash.utils.Dictionary;
   import fullshit.tool.Attach;
   
   public class GData
   {
      
      public var gdata:Dictionary;
      
      public var mcList:Object;
      
      public function GData()
      {
         super();
         this.gdata = new Dictionary();
         this.initData();
      }
      
      public function initListBG(param1:Array, param2:BGMoveSimple) : void
      {
         var _loc3_:int = 0;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:BMPDMCData2 = null;
         var _loc9_:Array = null;
         var _loc10_:int = 0;
         var _loc11_:int = 0;
         var _loc12_:Array = null;
         if(this.mcList)
         {
            this.dispose();
         }
         this.mcList = new Object();
         for each(_loc12_ in param1)
         {
            _loc3_ = int(_loc12_[0]);
            _loc4_ = Number(_loc12_[1]);
            _loc5_ = Number(_loc12_[2]);
            _loc6_ = Number(_loc12_[3]);
            _loc7_ = Number(_loc12_[4]);
            param2.cpx.addBGAt(_loc4_,_loc5_,_loc3_);
            _loc11_ = int(_loc12_.length);
            _loc10_ = 4;
            while(++_loc10_ < _loc11_)
            {
               _loc9_ = _loc12_[_loc10_];
               if(!this.mcList[_loc9_[0]])
               {
                  _loc8_ = MC2BMPD.mc2bmpd2Ani(Attach.attach(_loc9_[0]) as MovieClip,_loc9_[0],0);
                  this.mcList[_loc9_[0]] = _loc8_;
               }
               param2.cpx.addBmpd2dAt((this.mcList[_loc9_[0]] as BMPDMCData2).makeBMPD2D(true),_loc9_[1],_loc9_[2],_loc6_,_loc7_,_loc3_);
            }
         }
      }
      
      public function initListFly(param1:Array) : void
      {
         var _loc2_:F4DataLinks = null;
         for each(_loc2_ in param1)
         {
            this.f4linkSub(_loc2_,_loc2_);
         }
      }
      
      public function dispose() : void
      {
         var _loc1_:BMPDMCData2 = null;
         for each(_loc1_ in this.mcList)
         {
            _loc1_.bmpd1.bmpd.dispose();
         }
      }
      
      public function f4linkSub(param1:F4DataLinks, param2:F4DataLinks) : void
      {
         var _loc3_:BMPDMCData2 = null;
         var _loc4_:F4DataHole = null;
         var _key:String = param1.f4dataorg.mcName + param1.f4dataorg.lr + "_" + param1.f4dataorg.rot + "_" + param2.colorHue;
         if(!this.mcList[_key])
         {
            _loc3_ = MC2BMPD.mc2bmpd2Single(Attach.attach(param1.f4dataorg.mcName) as MovieClip,param1.f4dataorg.mcName,true,param1.f4dataorg.lr,param2.colorHue,true,param1.f4dataorg.rot);
            this.mcList[_key] = _loc3_;
         }
         for each(_loc4_ in param1.f4dataorg.hole)
         {
            if(_loc4_.partf4)
            {
               this.f4linkSub(_loc4_.partf4,param2);
            }
         }
      }
      
      public function f4Blt(param1:String, param2:int) : void
      {
         var _loc3_:BMPDMCData2 = null;
         if(!this.mcList[param1 + "_" + param2])
         {
            _loc3_ = MC2BMPD.mc2bmpd2Single(Attach.attach(param1) as MovieClip,param1,true,2,param2);
            this.mcList[param1 + "_" + param2] = _loc3_;
         }
      }
      
      public function initData() : void
      {
         var _loc1_:BMPDMCData2 = null;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It1") as MovieClip,"It1",0);
         this.gdata["fuel"] = _loc1_;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It2") as MovieClip,"It2",0);
         this.gdata["money"] = _loc1_;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It3") as MovieClip,"It3",0);
         this.gdata["life"] = _loc1_;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It4") as MovieClip,"It4",0);
         this.gdata["part"] = _loc1_;
      }
   }
}

