package f4.level
{
   import f4.core.OpOneCore;
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.evts.EvtOne;
   import f4data.F4DataLinks;
   import f4data.sdata.ConfigAndStory;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class LevelBase
   {
      
      public var targets:Targets;
      
      public var core:OpOneCore;
      
      protected var allEvts:Array;
      
      public function LevelBase()
      {
         super();
         this.targets = InitData.TARGETS;
         this.core = InitData.CORE;
         this.allEvts = [];
      }
      
      public function initFromObj(param1:Object) : void
      {
         var _loc2_:Object = null;
         this.core.levelData.initLevel(param1["cam"][0],param1["cam"][1],param1["cam"][2]);
         this.core.cam = this.core.renderData.cam;
         for each(_loc2_ in param1["evts"])
         {
            this.allEvts.push(this.readObjEvt(_loc2_));
         }
         for each(_loc2_ in param1["acts"])
         {
            this.readObjAct(_loc2_);
         }
         this.targets.runTargets();
         if(param1["talk"])
         {
            this.targets.evts.talkObj = param1["talk"];
         }
         this.linkEvts();
      }
      
      public function initEvts1st() : void
      {
      }
      
      public function linkEvts() : void
      {
         var _loc1_:EvtOne = null;
         var _loc2_:EvtOne = null;
         for each(_loc1_ in this.allEvts)
         {
            for each(_loc2_ in this.allEvts)
            {
               if(_loc1_ != _loc2_)
               {
                  if(Boolean(_loc1_.after) && _loc1_.after == _loc2_.evtName)
                  {
                     if(_loc1_.evtName.indexOf("clock") != -1)
                     {
                        _loc2_.finishAddClock.push(_loc1_);
                     }
                     else
                     {
                        _loc2_.finishAdd.push(_loc1_);
                     }
                  }
               }
            }
            if(!_loc1_.after)
            {
               if(_loc1_.evtName.indexOf("clock") != -1)
               {
                  this.targets.evts.clockEvts.push(_loc1_);
               }
               else
               {
                  this.targets.evts.evts[_loc1_.evtName] = _loc1_;
                  if(_loc1_.evtName.indexOf("toPlace") != -1)
                  {
                     this.targets.evts.spoint = new Point(_loc1_.data["rectPlace"].x + _loc1_.data["rectPlace"].width / 2,_loc1_.data["rectPlace"].y + _loc1_.data["rectPlace"].height / 2);
                     this.targets.evts.ef = null;
                  }
               }
            }
         }
      }
      
      public function readObjAct(param1:Object) : void
      {
         var _loc4_:F4DataLinks = null;
         var _loc2_:Object = param1["data"];
         var _loc3_:String = param1["name"];
         if(_loc3_ == "add")
         {
            this.targets.evts.add(_loc2_.xp,_loc2_.yp,_loc2_.iaim,_loc2_.pilot,_loc2_.hue,_loc2_.defIndex,_loc2_.friend,_loc2_.ai,_loc2_.aidata,_loc2_.pl);
         }
         else if(_loc3_ == "out")
         {
            this.targets.evts.addOuter(_loc2_.xp,_loc2_.yp,_loc2_.iaim,_loc2_.pilot,_loc2_.hue,_loc2_.defIndex,_loc2_.imax,_loc2_.ilim,_loc2_.ff,_loc2_.sf,_loc2_.fe,_loc2_.ai,_loc2_.aidata);
         }
         else if(_loc3_ == "player")
         {
            if(_loc2_.def)
            {
               _loc4_ = ConfigAndStory.ALLPART.defFly[_loc2_.index];
            }
            else
            {
               _loc4_ = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER];
               if(!_loc4_)
               {
                  _loc4_ = ConfigAndStory.ALLPART.defFly[40];
               }
            }
            _loc4_.colorHue = _loc2_.hue;
            InitData.GDATA.f4linkSub(_loc4_,_loc4_);
            this.core.playerFly = InitData.MAKER.makeAIFlyByData(_loc2_.xp,_loc2_.yp,_loc4_,true,_loc2_.mode < 2,true);
            this.core.playerFly.unity.move.autof = InitData.autof;
            this.core.playerFly.pilotName = _loc2_.pilot;
            this.core.playerFly.aimode = InitData.AIDATA.ais[_loc2_.aidata];
            this.core.playerFly.aimodeNum = _loc2_.aidata;
            if(_loc2_.iaim == "player")
            {
               this.core.playerFly.initAI(this.core.cam.crossPoint);
            }
            else
            {
               this.core.playerFly.initAI(new Point(_loc2_.iaim[0],_loc2_.iaim[1]));
            }
            this.core.cam.mv = this.core.playerFly.unity.move;
            this.core.playerFly.ignoreAI = _loc2_.ai;
            this.core.playerCtrlMode = _loc2_.mode;
            this.core.playerFly.fromDef = _loc2_.def;
            if(_loc2_.mode == 2)
            {
               this.core.playerFly.linkUnityTargetA(this.core.playerFly,this.core.cam.crossPoint);
               this.core.cam.screenMoveNum = 2;
            }
            InitData.TARGETS.startPoint.x = _loc2_.xp;
            InitData.TARGETS.startPoint.y = _loc2_.yp;
         }
      }
      
      public function readObjEvt(param1:Object) : EvtOne
      {
         var _loc2_:EvtOne = new EvtOne("");
         _loc2_.evtName = param1["name"];
         if(param1["data"])
         {
            _loc2_.data = param1["data"];
            if(_loc2_.data["rectPlace"])
            {
               _loc2_.data["rectPlace"] = new Rectangle(_loc2_.data["rectPlace"][0] - _loc2_.data["rectPlace"][2] / 2,_loc2_.data["rectPlace"][1] - _loc2_.data["rectPlace"][3] / 2,_loc2_.data["rectPlace"][2],_loc2_.data["rectPlace"][3]);
               trace(_loc2_.data["rectPlace"]);
            }
         }
         if(param1["require"])
         {
            _loc2_.require = param1["require"];
         }
         if(param1["finish"])
         {
            _loc2_.finish = param1["finish"];
         }
         if(param1["finishRemoveCF"])
         {
            _loc2_.finishRemoveCF = param1["finishRemoveCF"];
         }
         if(param1["finishRemove"])
         {
            _loc2_.finishRemove = param1["finishRemove"];
         }
         if(param1["finishRemoveClock"])
         {
            _loc2_.finishRemoveClock = param1["finishRemoveClock"];
         }
         if(param1["finishAdd"])
         {
            _loc2_.finishAdd = param1["finishAdd"];
         }
         if(param1["finishAddClock"])
         {
            _loc2_.finishAddClock = param1["finishAddClock"];
         }
         if(param1["timeLimit"])
         {
            _loc2_.timeLimit = param1["timeLimit"];
         }
         if(param1["looptime"])
         {
            _loc2_.looptime = param1["looptime"];
         }
         if(param1["always"])
         {
            _loc2_.always = param1["always"];
         }
         if(param1["after"])
         {
            _loc2_.after = param1["after"];
         }
         if(param1["finishActs"])
         {
            _loc2_.finishActs = param1["finishActs"];
         }
         if(param1["frameActs"])
         {
            _loc2_.frameActs = param1["frameActs"];
         }
         return _loc2_;
      }
   }
}

