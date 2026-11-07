package f4.evts
{
   import f4.core.OpOneCore;
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.data.sound32.SoundUBlt32;
   import f4.unit.ai.AIPart;
   import f4.unit.ai.AIUnity;
   import f4.unit.move.MoveAs;
   import f4.unit.outer.AIOuter;
   import f4.unit.particle.Item;
   import f4.unit.particle.Particle;
   import f4.unit.parts.SimpleFly;
   import f4.unit.upgrade.FlyData;
   import f4data.F4DataBullet;
   import f4data.F4DataLinks;
   import f4data.sdata.ConfigAndStory;
   import flash.display.MovieClip;
   import flash.geom.Point;
   import flash.utils.Dictionary;
   import fullshit.tool.Attach;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   import ui.pages.UIAllWin;
   import ui.pages.UIFail;
   import ui.pages.UIGame;
   import ui.pages.UIHelpX2;
   import ui.pages.UIMain;
   import ui.pages.UIModeX;
   import ui.pages.UITalk;
   import ui.pages.UIWin;
   
   public class EvtCtrl
   {
      
      public var clockEvts:Array;
      
      public var evts:Dictionary;
      
      public var finished:Dictionary;
      
      private var _rect2:Rect2;
      
      public var core:OpOneCore;
      
      public var gameui:UIGame;
      
      public var spoint:Point;
      
      public var ef:AIUnity;
      
      public var timer:uint;
      
      public var talkObj:Array = null;
      
      public function EvtCtrl()
      {
         super();
         this.timer = 0;
         this._rect2 = StaticShit.RECT2;
         this.evts = new Dictionary();
         this.finished = new Dictionary();
         this.clockEvts = [];
         this.spoint = null;
         this.ef = null;
      }
      
      public function talkSA() : void
      {
         if(this.talkObj)
         {
            this.startTalk(this.talkObj);
         }
         this.talkObj = null;
      }
      
      public function clean() : void
      {
         this.timer = 0;
         this.evts = new Dictionary();
         this.finished = new Dictionary();
         this.clockEvts.length = 0;
         this.spoint = null;
         this.ef = null;
      }
      
      public function ckDicAndEnd(param1:EvtOne, param2:*) : void
      {
         var _loc4_:String = null;
         var _loc5_:EvtOne = null;
         var _loc6_:int = 0;
         var _loc7_:int = 0;
         var _loc8_:Array = null;
         var _loc3_:Boolean = true;
         for each(_loc4_ in param1.require)
         {
            if(this.finished[_loc4_] == null)
            {
               _loc3_ = false;
               break;
            }
         }
         if(_loc3_)
         {
            for each(_loc4_ in param1.finish)
            {
               this.finished[_loc4_] = 1;
               trace(_loc4_,"f add");
            }
            for each(_loc5_ in param1.finishAdd)
            {
               this.evts[_loc5_.evtName] = _loc5_;
               if(_loc5_.evtName.indexOf("toPlace") != -1)
               {
                  this.spoint = new Point(_loc5_.data["rectPlace"].x + _loc5_.data["rectPlace"].width / 2,_loc5_.data["rectPlace"].y + _loc5_.data["rectPlace"].height / 2);
                  this.ef = null;
               }
               trace(_loc5_.evtName,"fe add");
            }
            for each(_loc5_ in param1.finishAddClock)
            {
               this.clockEvts.push(_loc5_);
               trace(_loc5_.evtName,"fc add");
            }
            for each(_loc4_ in param1.finishRemoveCF)
            {
               this.finished[_loc4_] = null;
               trace(_loc4_,"f remvoe");
            }
            for each(_loc4_ in param1.finishRemove)
            {
               this.evts[_loc4_] = null;
               trace(_loc4_,"fe remvoe");
            }
            for each(_loc4_ in param1.finishRemoveClock)
            {
               trace(_loc4_,"fc remvoe");
               _loc7_ = int(param1.finishRemoveClock.length);
               _loc6_ = 0;
               while(_loc6_ < _loc7_)
               {
                  if(param1.finishRemoveClock[_loc6_].evtName == _loc4_)
                  {
                     param1.finishRemoveClock.splice(_loc6_,1);
                     break;
                  }
                  _loc6_++;
               }
            }
            for each(_loc8_ in param1.finishActs)
            {
               this.evtAct(_loc8_[0],_loc8_[1]);
            }
            --param1.looptime;
            if(!param1.always && param1.looptime <= 0)
            {
               trace(param1.evtName,"end");
               this.evts[param1.evtName] = null;
            }
         }
      }
      
      public function getItem(param1:String) : void
      {
         var _loc2_:EvtOne = null;
         _loc2_ = this.evts["item_" + param1];
         if(_loc2_)
         {
            this.ckDicAndEnd(_loc2_,param1);
         }
      }
      
      public function toPlaceAct(param1:AIUnity, param2:String) : void
      {
         var _loc3_:EvtOne = null;
         _loc3_ = this.evts["toPlace_" + param2];
         if(_loc3_)
         {
            if(this._rect2.xyInRect(param1.unity.move.point.x,param1.unity.move.point.y,_loc3_.data["rectPlace"]))
            {
               this.ckDicAndEnd(_loc3_,param1);
               this.spoint = null;
               trace("atplace");
            }
         }
      }
      
      public function uDeadAct(param1:AIUnity, param2:String, param3:AIUnity) : void
      {
         var _loc4_:EvtOne = null;
         var _loc5_:String = null;
         var _loc6_:Boolean = false;
         if(param1.unity.friend)
         {
            _loc4_ = this.evts["uDead_" + param2 + "_P"];
         }
         else
         {
            _loc4_ = this.evts["uDead_" + param2 + "_E"];
         }
         if(!_loc4_)
         {
            if(param1.unity.friend)
            {
               _loc4_ = this.evts["uDead_all_P"];
            }
            else
            {
               _loc4_ = this.evts["uDead_all_E"];
            }
         }
         if(_loc4_)
         {
            _loc6_ = _loc4_.data["fireName"] == "all";
            if(!_loc6_)
            {
               for each(_loc5_ in param3.pilotName)
               {
                  if((_loc4_.data["fireName"] as Array).indexOf(_loc5_) != -1)
                  {
                     _loc6_ = true;
                     break;
                  }
               }
            }
            if(_loc6_)
            {
               ++_loc4_.data["num"];
               trace(param1.pilotName,_loc4_.data["num"],_loc4_.data["rnum"]);
               if(_loc4_.data["num"] >= _loc4_.data["rnum"])
               {
                  this.ckDicAndEnd(_loc4_,param1);
               }
            }
         }
      }
      
      public function runClock() : void
      {
         var _loc1_:EvtOne = null;
         var _loc5_:Array = null;
         var _loc6_:int = 0;
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:int = int(this.clockEvts.length);
         while(_loc2_ < _loc4_)
         {
            _loc1_ = this.clockEvts[_loc2_];
            _loc6_ = int(_loc1_.data["time"]);
            for each(_loc5_ in _loc1_.frameActs)
            {
               if(_loc6_ % _loc5_[1] == 0)
               {
                  this.evtAct(_loc5_[0],_loc5_[2]);
               }
            }
            if(_loc1_.data["time"]-- <= 0)
            {
               this.ckDicAndEnd(_loc1_,null);
               this.clockEvts[_loc2_] = this.clockEvts[--_loc4_];
               _loc3_++;
            }
            else
            {
               _loc2_++;
            }
         }
         var _loc7_:int = int(this.clockEvts.length);
         if(_loc3_ > 0)
         {
            _loc2_ = -1;
            while(++_loc2_ < _loc3_)
            {
               this.clockEvts[_loc4_ + _loc2_] = this.clockEvts[_loc7_ - 1 - _loc2_];
            }
         }
         this.clockEvts.length = _loc7_ - _loc3_;
      }
      
      public function evtAct(param1:String, param2:*) : void
      {
         if(param1 == "kill")
         {
            this.kill(param2);
         }
         else if(param1 == "add")
         {
            this.add(param2.xp,param2.yp,param2.iaim,param2.pilot,param2.hue,param2.defIndex,param2.friend,param2.ai,param2.aidata,param2["pl"]);
         }
         else if(param1 == "out")
         {
            this.addOuter(param2.xp,param2.yp,param2.iaim,param2.pilot,param2.hue,param2.defIndex,param2.imax,param2.ilim,param2.ff,param2.sf,param2.fe,param2.ai,param2.aidata);
         }
         else if(param1 == "windir")
         {
            trace("act windir");
            this.win();
         }
         else if(param1 == "money")
         {
            this.addMoney();
         }
         else if(param1 == "talk")
         {
            this.startTalk(param2);
         }
         else if(param1 == "aimp")
         {
            this.findPersonAndChangeAim(param2["name"],param2["x"],param2["y"],param2["ai"]);
         }
         else if(param1 == "aimu")
         {
            this.findPersonAndChangeAimAI(param2["name"],param2["aim"],param2["ai"]);
         }
         else if(param1 == "fire")
         {
            this.fireSomething(param2["x"],param2["y"],param2["spd"],param2["spda"],param2["facc"],param2["acc"],param2["acca"],param2["blt"],param2["who"],param2["aim"],param2["friend"]);
         }
         else if(param1 == "fireAB")
         {
            this.fireA2B(param2["a"],param2["b"],param2["spd"],param2["facc"],param2["acc"],param2["blt"],param2["aim"],param2["friend"]);
         }
         else if(param1 == "giveItem")
         {
            this.giveItem(param2.who,param2.what);
         }
         else if(param1 == "lose")
         {
            this.loseNow();
         }
         else if(param1 == "delLose")
         {
            this.delAllEvtAddLose();
         }
         else if(param1 == "spwho")
         {
            this.spointToWho(param2.who);
         }
         else if(param1 == "spitem")
         {
            this.spointToI1();
         }
         else if(param1 == "cam")
         {
            this.findPersonAndCamMove(param2["name"]);
         }
         else if(param1 == "god")
         {
            this.findPersonAndGod(param2["name"]);
         }
         else if(param1 == "cleanE")
         {
            this.cleanE();
         }
         else if(param1 == "tmove")
         {
            this.tempMove(param2.name,param2.x,param2.y);
         }
         else if(param1 == "mode")
         {
            this.changeCoreMode(param2.mode);
         }
         else if(param1 == "nohit")
         {
            this.noHit(param2.name,param2.nohit);
         }
         else if(param1 == "help")
         {
            this.helpStop();
         }
      }
      
      public function helpStop() : void
      {
         InitData.CORE.pause = true;
         new UIHelpX2(7,"How to play",this.gameui.sp,Attach.attach("UI_HelpGame2") as MovieClip,"UI_HelpGame2",this.gameui.uictrl,this.gameui,this.resume);
      }
      
      public function resume() : void
      {
         InitData.CORE.pause = false;
      }
      
      public function cleanE() : void
      {
         var _loc1_:AIUnity = null;
         var _loc2_:AIOuter = null;
         for each(_loc1_ in this.core.targets.efa1.array)
         {
            _loc1_.kill();
         }
         for each(_loc2_ in this.core.targets.outa1.array)
         {
            _loc2_.isDeadA1 = true;
         }
      }
      
      public function changeCoreMode(param1:int) : void
      {
         this.core.playerCtrlMode = param1;
         if(param1 < 2)
         {
            this.core.playerFly.close = 1;
            this.core.playerFly.aim = this.core.cam.crossPoint;
            this.core.playerFly.aimSecond = this.core.cam.crossPoint;
            this.gameui.mov.getChildByName("stand").visible = false;
            (this.gameui.mouse.getChildByName("mid") as MovieClip).gotoAndStop(1);
            (this.gameui.mouse.getChildByName("side") as MovieClip).gotoAndStop(1);
         }
         this.gameui.changeKM();
      }
      
      public function kill(param1:String) : void
      {
         var _loc2_:AIUnity = null;
         if(this.core.playerFly.pilotName.indexOf(param1) != -1)
         {
            this.core.playerFly.kill();
         }
         for each(_loc2_ in this.core.targets.pfa1.array)
         {
            if(_loc2_.pilotName.indexOf(param1) != -1)
            {
               _loc2_.kill();
            }
         }
         for each(_loc2_ in this.core.targets.efa1.array)
         {
            if(_loc2_.pilotName.indexOf(param1) != -1)
            {
               _loc2_.kill();
            }
         }
      }
      
      public function add(param1:Number, param2:Number, param3:String, param4:Array, param5:int, param6:int, param7:Boolean, param8:Boolean, param9:int, param10:Boolean) : void
      {
         var _loc12_:Point = null;
         var _loc13_:F4DataLinks = null;
         var _loc11_:AIUnity = this.getAIByPerson(param3);
         if(_loc11_)
         {
            _loc12_ = _loc11_.unity.move.point;
         }
         else
         {
            _loc12_ = new Point(param1,param2);
         }
         if(!param10)
         {
            if(param4 != null && (param4 is Array || param4 is String) && param4.indexOf("Captain") != -1)
            {
               _loc13_ = ConfigAndStory.getCaptainFly();
               if(!_loc13_)
               {
                  _loc13_ = ConfigAndStory.ALLPART.defFly[param6];
               }
            }
            else
            {
               _loc13_ = ConfigAndStory.ALLPART.defFly[param6];
            }
         }
         else if(param6 == -1)
         {
            _loc13_ = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER];
         }
         else
         {
            _loc13_ = ConfigAndStory.ALLPART.plFly[param6];
         }
         _loc13_.colorHue = param5;
         InitData.GDATA.f4linkSub(_loc13_,_loc13_);
         var _loc14_:AIUnity = InitData.MAKER.makeAIFlyByData(param1,param2,_loc13_,param7);
         _loc14_.ignoreAI = param8;
         _loc14_.unity.move.autof = InitData.autof;
         _loc14_.initAI(_loc12_);
         _loc14_.pilotName = param4;
         if(param4 != null && (param4 is Array || param4 is String) && param4.indexOf("Captain") != -1)
         {
            _loc14_.isCaptain = true;
            _loc14_.tacticalMode = 0;
            _loc14_.spawnPoint = new Point(param1,param2);
         }
         _loc14_.aimode = InitData.AIDATA.ais[param9];
         _loc14_.aimodeNum = param9;
         if(param7)
         {
            this.core.targets.pfa1.addOne1(_loc14_);
         }
         else
         {
            this.core.targets.efa1.addOne1(_loc14_);
         }
      }
      
      public function addAI(param1:Number, param2:Number, param3:String, param4:Array, param5:int, param6:int, param7:Boolean, param8:Boolean, param9:int) : void
      {
         var _loc11_:Point = null;
         var _loc14_:Particle = null;
         var _loc10_:AIUnity = this.getAIByPerson(param3);
         if(_loc10_)
         {
            _loc11_ = _loc10_.unity.move.point;
         }
         else
         {
            _loc11_ = new Point(param1,param2);
         }
         var _loc12_:F4DataLinks = ConfigAndStory.ALLPART.plFly[param6];
         _loc12_.colorHue = param5;
         InitData.GDATA.f4linkSub(_loc12_,_loc12_);
         var _loc13_:AIUnity = InitData.MAKER.makeAIFlyByData(param1,param2,_loc12_,param7);
         _loc13_.ignoreAI = param8;
         _loc13_.unity.move.autof = InitData.autof;
         _loc13_.initAI(_loc11_);
         _loc13_.pilotName = param4;
         _loc13_.aimode = InitData.AIDATA.ais[param9];
         _loc13_.aimodeNum = param9;
         if(param7)
         {
            this.core.targets.pfa1.addOne1(_loc13_);
         }
         else
         {
            this.core.targets.efa1.addOne1(_loc13_);
         }
         _loc13_.fromDef = false;
         _loc13_.buildHitAll();
         var _loc15_:Targets = InitData.TARGETS;
         var _loc16_:int = 0;
         while(_loc16_ < 15)
         {
            _loc14_ = new Particle(InitData.CORE.cam,Math.random() * _loc13_.unity.move.hitRect.width + _loc13_.unity.move.hitRect.x,Math.random() * _loc13_.unity.move.hitRect.height + _loc13_.unity.move.hitRect.y,Math.random() * Math.PI * 2,Math.random() * 0.8,1,0,0.999,Math.random() * 50 + 20);
            _loc14_.layer = 1;
            _loc14_.flash = true;
            _loc14_.color = 16777215;
            _loc15_.pa1.addOne1(_loc14_);
            _loc16_++;
         }
      }
      
      public function addOuter(param1:Number, param2:Number, param3:String, param4:Array, param5:int, param6:int, param7:int, param8:int, param9:int, param10:int, param11:int, param12:Boolean, param13:int) : void
      {
         var _loc15_:Point = null;
         var _loc14_:AIUnity = this.getAIByPerson(param3);
         if(_loc14_)
         {
            _loc15_ = _loc14_.unity.move.point;
         }
         else
         {
            _loc15_ = new Point(param1,param2);
         }
         var _loc16_:F4DataLinks = ConfigAndStory.ALLPART.defFly[param6];
         var _loc17_:AIOuter = new AIOuter(param1,param2,_loc15_,param5,_loc16_,param4,param7,param8,param9,param10,param11,param12,param13);
         this.core.targets.outa1.addOne1(_loc17_);
      }
      
      public function addTime() : void
      {
      }
      
      public function win() : void
      {
         var _loc1_:Item = null;
         if(ConfigAndStory.STORY < ConfigAndStory.MAP1)
         {
            if(ConfigAndStory.ALLPART.repairPart.length == 0)
            {
               ConfigAndStory.ALLPART.levelMoney += ConfigAndStory.STORY * 5454;
            }
            else
            {
               ConfigAndStory.ALLPART.levelMoney += ConfigAndStory.STORY * 2121;
            }
         }
         else
         {
            ConfigAndStory.ALLPART.levelMoney += this.timer / 60 / 60 * 5656;
            ConfigAndStory.ALLPART.levelMoney *= 5;
         }
         InitData.SOUNDC2.allTo(0,-0.1,true);
         for each(_loc1_ in this.core.targets.ia1.array)
         {
            _loc1_.beHit(this.core.playerFly);
         }
         InitData.CORE.winAct(true);
         if(ConfigAndStory.STORY == 34)
         {
            new UIAllWin(this.gameui.sp,Attach.attach("UI_AllWin") as MovieClip,"UI_AllWin",this.gameui.uictrl);
         }
         else if(ConfigAndStory.STORY == 49)
         {
            if(!ConfigAndStory.S100LOCK)
            {
               ConfigAndStory.STORY = 99;
               ConfigAndStory.saveData();
               ConfigAndStory.save();
            }
            else
            {
               ConfigAndStory.S100LOCK = false;
               ConfigAndStory.STORY = ConfigAndStory.S100LOCKINT;
            }
            new UIMain(this.gameui.sp,Attach.attach("UI_Main") as MovieClip,"UI_Main",this.gameui.uictrl);
         }
         else if(ConfigAndStory.STORY == 99)
         {
            new UIModeX(this.gameui.sp,Attach.attach("UI_ModeX") as MovieClip,"UI_ModeX",this.gameui.uictrl);
         }
         else
         {
            new UIWin(this.gameui.sp,Attach.attach("UI_Win") as MovieClip,"UI_Win",this.gameui.uictrl);
         }
         this.gameui.removeAll();
      }
      
      public function addMoney() : void
      {
      }
      
      public function cam2u(param1:MoveAs) : void
      {
         this.core.cam.mv = param1;
         trace("move cam");
      }
      
      public function endTalk() : void
      {
         this.core.pause = false;
      }
      
      public function startTalk(param1:Array) : void
      {
         this.core.pause = true;
         var _loc2_:UITalk = new UITalk(this.endTalk,param1,this.gameui.sp,Attach.attach("UI_Talk") as MovieClip,"UI_Talk",this.gameui.uictrl,this.gameui,this);
      }
      
      public function getAIByPerson(param1:String) : AIUnity
      {
         var _loc2_:AIUnity = null;
         var _loc3_:AIUnity = null;
         if(this.core.playerFly.pilotName.indexOf(param1) != -1)
         {
            return this.core.playerFly;
         }
         if(!_loc2_)
         {
            for each(_loc3_ in this.core.targets.pfa1.array)
            {
               if(_loc3_.pilotName.indexOf(param1) != -1)
               {
                  return _loc3_;
               }
            }
         }
         if(!_loc2_)
         {
            for each(_loc3_ in this.core.targets.efa1.array)
            {
               if(_loc3_.pilotName.indexOf(param1) != -1)
               {
                  return _loc3_;
               }
            }
         }
         return null;
      }
      
      public function noHit(param1:String, param2:Boolean) : void
      {
         var _loc3_:AIUnity = this.getAIByPerson(param1);
         if(_loc3_)
         {
            _loc3_.nohit = param2;
         }
      }
      
      public function tempMove(param1:String, param2:Number, param3:Number) : void
      {
         var _loc4_:AIUnity = this.getAIByPerson(param1);
         if(_loc4_)
         {
            _loc4_.unity.move.point.x = param2;
            _loc4_.unity.move.point.y = param3;
         }
      }
      
      public function findPersonAndGod(param1:String) : void
      {
         var _loc2_:AIUnity = this.getAIByPerson(param1);
         if(_loc2_)
         {
            _loc2_.god = true;
            _loc2_.linkGod();
         }
      }
      
      public function findPersonAndCamMove(param1:String) : void
      {
         var _loc2_:AIUnity = this.getAIByPerson(param1);
         if(_loc2_)
         {
            this.cam2u(_loc2_.unity.move);
         }
      }
      
      public function camPlayer() : void
      {
         var _loc1_:AIUnity = this.core.playerFly;
         if(_loc1_)
         {
            this.cam2u(_loc1_.unity.move);
         }
      }
      
      public function findPersonAndChangeAim(param1:String, param2:Number, param3:Number, param4:Boolean) : void
      {
         var _loc5_:AIUnity = this.getAIByPerson(param1);
         if(_loc5_)
         {
            _loc5_.ignoreAI = param4;
            _loc5_.initAI(new Point(param2,param3));
         }
      }
      
      public function findPersonAndChangeAimAI(param1:String, param2:String, param3:Boolean) : void
      {
         var _loc4_:AIUnity = this.getAIByPerson(param1);
         var _loc5_:AIUnity = this.getAIByPerson(param2);
         if(Boolean(_loc4_) && Boolean(_loc5_))
         {
            _loc4_.ignoreAI = param3;
            _loc4_.initAI(_loc5_.unity.move.point);
         }
      }
      
      public function fireSomething(param1:Number, param2:Number, param3:Number, param4:Number, param5:Number, param6:Number, param7:Number, param8:int, param9:String, param10:String, param11:Boolean) : void
      {
         var _loc15_:int = 0;
         var _loc17_:SoundUBlt32 = null;
         var _loc18_:Number = NaN;
         var _loc19_:int = 0;
         var _loc12_:AIUnity = this.getAIByPerson(param10);
         var _loc13_:AIUnity = this.getAIByPerson(param9);
         var _loc14_:F4DataBullet = ConfigAndStory.ALLPART.defBullet[param8];
         if(_loc13_)
         {
            _loc15_ = (_loc13_.unity.parts[0] as AIPart).hue + 180;
         }
         var _loc16_:SimpleFly = InitData.MAKER.makeFly(param1 - (this.core.cam.rect.x - this.core.cam.last.x),param2 - (this.core.cam.rect.y - this.core.cam.last.y),_loc13_,_loc14_,_loc15_);
         _loc16_.move.initSpd(param3,(param4 + (param5 * Math.random() - param5 / 2)) / 180 * Math.PI,param6,param7 / 180 * Math.PI);
         _loc16_.data = new FlyData(_loc14_.name,new Point(_loc14_.hp,_loc14_.dmg),new Point(),new Point(),0,_loc14_.mass);
         _loc16_.move.makeHitRectFromWH(_loc14_.hit.x,_loc14_.hit.y);
         _loc16_.life = _loc14_.life;
         if(_loc12_)
         {
            _loc16_.aim = _loc12_.unity.move.point;
         }
         _loc16_.render.changeColor(25);
         if(param11)
         {
            this.core.targets.pba1.addOne1(_loc16_);
         }
         else
         {
            this.core.targets.eba1.addOne1(_loc16_);
         }
         if(_loc14_.btwh < 5)
         {
            _loc18_ = 0.5;
            _loc19_ = 2;
         }
         else
         {
            _loc18_ = 1;
            _loc19_ = 0;
         }
         _loc17_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[_loc19_],_loc19_,new Point(this.core.cam.rect.x + this.core.cam.rect.width / 2,this.core.cam.rect.y + this.core.cam.rect.height / 2));
         _loc17_.volMut = _loc18_;
         InitData.SOUNDC32.addSFX25(_loc17_);
      }
      
      public function fireA2B(param1:String, param2:String, param3:Number, param4:Number, param5:Number, param6:int, param7:String, param8:Boolean) : void
      {
         var _loc12_:F4DataBullet = null;
         var _loc13_:int = 0;
         var _loc14_:Number = NaN;
         var _loc15_:Number = NaN;
         var _loc16_:Number = NaN;
         var _loc17_:Number = NaN;
         var _loc18_:Number = NaN;
         var _loc19_:SimpleFly = null;
         var _loc20_:SoundUBlt32 = null;
         var _loc21_:Number = NaN;
         var _loc22_:int = 0;
         var _loc9_:AIUnity = this.getAIByPerson(param7);
         var _loc10_:AIUnity = this.getAIByPerson(param1);
         var _loc11_:AIUnity = this.getAIByPerson(param2);
         if(Boolean(_loc10_) && Boolean(_loc11_))
         {
            _loc12_ = ConfigAndStory.ALLPART.defBullet[param6];
            _loc13_ = (_loc10_.unity.parts[0] as AIPart).hue + 180;
            _loc14_ = _loc10_.unity.move.point.x;
            _loc15_ = _loc10_.unity.move.point.y;
            _loc16_ = _loc11_.unity.move.point.x;
            _loc17_ = _loc11_.unity.move.point.y;
            _loc18_ = Math.atan2(_loc17_ - _loc15_,_loc16_ - _loc14_) / Math.PI * 180;
            _loc19_ = InitData.MAKER.makeFly(_loc14_ - (this.core.cam.rect.x - this.core.cam.last.x),_loc15_ - (this.core.cam.rect.y - this.core.cam.last.y),_loc10_,_loc12_,_loc13_);
            _loc19_.move.initSpd(param3,(_loc18_ + (param4 * Math.random() - param4 / 2)) / 180 * Math.PI,param5,_loc18_ / 180 * Math.PI);
            _loc19_.data = new FlyData(_loc12_.name,new Point(_loc12_.hp,_loc12_.dmg),new Point(),new Point(),0,_loc12_.mass);
            _loc19_.move.makeHitRectFromWH(_loc12_.hit.x,_loc12_.hit.y);
            _loc19_.life = _loc12_.life;
            if(_loc9_)
            {
               _loc19_.aim = _loc9_.unity.move.point;
            }
            _loc19_.render.changeColor(_loc13_);
            if(param8)
            {
               this.core.targets.pba1.addOne1(_loc19_);
            }
            else
            {
               this.core.targets.eba1.addOne1(_loc19_);
            }
            if(_loc12_.btwh < 5)
            {
               _loc21_ = 0.5;
               _loc22_ = 2;
               this.core.cam.addShake(this.core.cam.rect.x + this.core.cam.rect.width / 2,this.core.cam.rect.y + this.core.cam.rect.height / 2,0.3);
            }
            else
            {
               _loc21_ = 1;
               _loc22_ = 0;
               this.core.cam.addShake(this.core.cam.rect.x + this.core.cam.rect.width / 2,this.core.cam.rect.y + this.core.cam.rect.height / 2,5);
            }
            _loc20_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[_loc22_],_loc22_,new Point(this.core.cam.rect.x + this.core.cam.rect.width / 2,this.core.cam.rect.y + this.core.cam.rect.height / 2));
            _loc20_.volMut = _loc21_;
            InitData.SOUNDC32.addSFX25(_loc20_);
         }
      }
      
      public function giveItem(param1:String, param2:String) : void
      {
         var _loc3_:AIUnity = this.getAIByPerson(param1);
         if(_loc3_)
         {
            _loc3_.itemArr.push([param2,0]);
         }
      }
      
      public function delAllEvtAddLose() : void
      {
         this.clean();
         var _loc1_:EvtOne = new EvtOne("clock_0");
         _loc1_.data = new Object();
         _loc1_.data["time"] = 60 * 5;
         _loc1_.finishActs.push(["lose",0]);
         this.clockEvts.push(_loc1_);
         InitData.SOUNDC2.allEnd(true);
      }
      
      public function loseNow() : void
      {
         InitData.CORE.winAct(false);
         new UIFail(this.gameui.sp,Attach.attach("UI_Fail") as MovieClip,"UI_Fail",this.gameui.uictrl);
         this.gameui.removeAll();
      }
      
      public function get nowTime() : String
      {
         var _loc2_:int = 0;
         if(ConfigAndStory.STORY == 99)
         {
            ++this.timer;
            return this.frameToTime(this.timer);
         }
         var _loc1_:EvtOne = this.clockEvts[0];
         if(Boolean(_loc1_) && _loc1_.evtName.indexOf("clockZ") != -1)
         {
            _loc2_ = int(_loc1_.data["time"]);
            return this.frameToTime(_loc2_);
         }
         return "---";
      }
      
      public function frameToTime(param1:int) : String
      {
         var _loc4_:String = null;
         var _loc2_:int = param1 / 60;
         var _loc3_:int = _loc2_ / 60;
         _loc2_ -= _loc3_ * 60;
         if(_loc2_ < 10)
         {
            _loc4_ = "0" + _loc2_;
         }
         else
         {
            _loc4_ = _loc2_ + "";
         }
         return _loc3_ + ":" + _loc4_;
      }
      
      public function spointToE1() : void
      {
         var _loc1_:AIUnity = null;
         for each(_loc1_ in InitData.TARGETS.efa1.array)
         {
            if(!_loc1_.isDeadA1 && !_loc1_.god)
            {
               this.ef = _loc1_;
               this.spoint = this.ef.unity.move.point;
               break;
            }
         }
      }
      
      public function spointToWho(param1:String) : void
      {
         var _loc2_:AIUnity = this.getAIByPerson(param1);
         if(_loc2_)
         {
            this.spoint = _loc2_.unity.move.point;
            this.ef = null;
         }
      }
      
      public function ckSpointFrame() : void
      {
         if(!this.spoint || Boolean(this.spoint && this.spoint.x < 0) && Boolean(this.spoint.y < 0))
         {
            this.spointToE1();
         }
         else if(Boolean(this.ef) && Boolean(this.ef.unity.move.point == this.spoint) && this.ef.god)
         {
            this.spointToE1();
         }
      }
      
      public function runSpoint() : void
      {
         this.ckSpointFrame();
      }
      
      public function runWinEvt() : void
      {
         var _loc1_:EvtOne = this.evts["win"];
         if(_loc1_)
         {
            if(this.core.targets.efa1.num == 0 && this.core.targets.outa1.num == 0)
            {
               this.ckDicAndEnd(_loc1_,null);
            }
         }
      }
      
      public function runWin1Evt() : void
      {
         var _loc1_:EvtOne = this.evts["win1"];
         if(_loc1_)
         {
            if(this.core.targets.efa1.num == 1 && this.core.targets.outa1.num == 0)
            {
               this.ckDicAndEnd(_loc1_,null);
            }
         }
      }
      
      public function spointToI1() : void
      {
         var _loc1_:Item = null;
         for each(_loc1_ in InitData.TARGETS.ia1.array)
         {
            if(!_loc1_.isDeadA1 && _loc1_.data[0] == "fuel")
            {
               this.spoint = _loc1_.point;
               this.ef = null;
               break;
            }
         }
      }
   }
}

