package ui.pages
{
   import f4.data.InitData;
   import f4data.F4DataLinks;
   import f4data.F4ShowData;
   import f4data.MC2F4Data;
   import f4data.cpx.F4DataCPX;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.ctrl2.SoundUnit2;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.unit.FormChooseFlyAI;
   import ui.unit.ShowPart;
   
   public class UIChooseFlyAI extends UIBase
   {
      
      public var form:FormChooseFlyAI;
      
      public var mc2f4:MC2F4Data;
      
      public var f4datax:F4DataCPX;
      
      public var dtLinkF:F4DataLinks;
      
      public var dtLinkS:F4DataLinks;
      
      public var ais:Array;
      
      public var aiXnum:Number;
      
      public var tempPlayerNum:int = -1;
      
      public var ainum:int = 20;
      
      public function UIChooseFlyAI(param1:F4DataCPX, param2:DisplayObjectContainer, param3:MovieClip, param4:String, param5:UICtrl, param6:UIBase = null)
      {
         ConfigAndStory.checkCaptainShip();
         var _capFly:F4DataLinks = ConfigAndStory.getCaptainFly();
         if(_capFly)
         {
            if(ConfigAndStory.PLAYER == _capFly.index)
            {
               ConfigAndStory.PLAYER = 0;
            }
            var _aiSlot:int = 0;
            while(_aiSlot < ConfigAndStory.AI.length)
            {
               if(ConfigAndStory.AI[_aiSlot] == _capFly.index)
               {
                  ConfigAndStory.AI[_aiSlot] = null;
               }
               _aiSlot++;
            }
         }
         param3.getChildByName("lv").visible = false;
         (param3.getChildByName("txtbt") as TextField).text = "BATTLE-" + (ConfigAndStory.STORY + 1);
         this.f4datax = param1;
         this.form = new FormChooseFlyAI(param3.getChildByName("form") as MovieClip,param1.plFly);
         (param3.getChildByName("lock1") as MovieClip).gotoAndStop(1);
         (param3.getChildByName("lock2") as MovieClip).gotoAndStop(1);
         this.form.updateAI();
         this.form.initsortMC();
         (param3.getChildByName("shopnew") as MovieClip).mouseEnabled = false;
         (param3.getChildByName("shopnew") as MovieClip).mouseChildren = false;
         if(!ConfigAndStory.SHOPNEW || ConfigAndStory.STORY == 1 || ConfigAndStory.STORY == 99)
         {
            param3.getChildByName("shopnew").visible = false;
         }
         ConfigAndStory.save();
         super(param2,param3,param4,param5,param6);
         this.mc2f4 = new MC2F4Data();
         this.ais = [];
         this.aiXnum = (_mov.getChildByName("form") as MovieClip).getChildByName("$choosesort3").x;
         this.fillAI();
         var _loc7_:F4DataLinks = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER];
         if(_loc7_)
         {
            this.addDtL(_loc7_,this.dtLinkF,"showFly",false);
         }
         InitData.SOUNDC2.allEnd(true,"M_Menu");
         InitData.SOUNDC2.addSound(new SoundUnit2(InitData.SOUNDDATA.musicArray[1],"M_Menu"),int.MAX_VALUE,0.01,InitData.SOUNDC2.nowVol);
      }
      
      override public function afterFadeIn() : void
      {
         var _loc1_:F4DataLinks = ConfigAndStory.ALLPART.plFly[ConfigAndStory.PLAYER];
         if(!_loc1_)
         {
            disableBtn("ready");
         }
         this.form.initsortMC();
         this.form.sort(0);
         this.form.sort(0);
         if(ConfigAndStory.STORY == 10)
         {
            if(ConfigAndStory.SHOWTEXTMENU[0] == 0)
            {
               ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,2,3,0);
               ConfigAndStory.SHOWTEXTMENU[0] = 1;
               ConfigAndStory.saveData();
            }
            else
            {
               this.afterTalk();
            }
         }
         else if(ConfigAndStory.SHOWTEXTMENU[1] == 0)
         {
            ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,2,0,0);
            ConfigAndStory.SHOWTEXTMENU[1] = 1;
            ConfigAndStory.saveData();
         }
         else
         {
            this.afterTalk();
         }
      }
      
      public function afterTalk() : void
      {
         super.afterFadeIn();
         if(ConfigAndStory.STORY >= 14)
         {
            (_mov.getChildByName("lock1") as MovieClip).gotoAndPlay(1);
            (_mov.getChildByName("lock2") as MovieClip).gotoAndPlay(1);
         }
         if(ConfigAndStory.STORY == 1)
         {
            if(ConfigAndStory.HOWTOPLAY[1] == 0)
            {
               new UIHelpX2(1,"Center",_sp,Attach.attach("UI_HelpBuildOneFirst") as MovieClip,uiName,_ui,this);
            }
         }
         else if(ConfigAndStory.STORY == 2)
         {
            if(ConfigAndStory.HOWTOPLAY[8] == 0)
            {
               new UIHelpX2(8,"Center",_sp,Attach.attach("UI_HelpOneSec") as MovieClip,uiName,_ui,this);
            }
         }
         else if(ConfigAndStory.STORY >= 14)
         {
            if(ConfigAndStory.HOWTOPLAY[2] == 0)
            {
               new UIHelpX2(2,"Center",_sp,Attach.attach("UI_HelpChooseCraftAI") as MovieClip,uiName,_ui,this);
            }
         }
      }
      
      override public function removeSelf() : UIBase
      {
         super.removeSelf();
         return _last;
      }
      
      override public function outAct(param1:String) : void
      {
         if(param1.indexOf("form") != -1)
         {
            this.form.disableLight();
         }
      }
      
      override public function overAct(param1:String) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataLinks = null;
         var _loc4_:int = 0;
         if(param1.indexOf("form") != -1)
         {
            _loc2_ = parseInt(param1.substr(4));
            _loc3_ = this.form.getLink(_loc2_);
            if(_loc3_)
            {
               this.addDtL(_loc3_,this.dtLinkS,"showDetail",true);
               this.form.lightOn(_loc2_);
            }
         }
         else if(param1.charAt(0) == "a")
         {
            _loc2_ = parseInt(param1.substr(1)) - 1;
            if(ConfigAndStory.AI[_loc2_])
            {
               _loc4_ = int(ConfigAndStory.AI[_loc2_]);
               _loc3_ = this.f4datax.plFly[_loc4_];
               if(_loc3_)
               {
                  this.addDtL(_loc3_,this.dtLinkS,"showDetail",true);
               }
            }
         }
      }
      
      override public function act(param1:BtnBase) : void
      {
         var sdata:F4ShowData = null;
         var sub:int = 0;
         var formNum:int = 0;
         var f4link:F4DataLinks = null;
         var plNum:int = 0;
         var parentStrAI:String = null;
         var formNumAI:int = 0;
         var f4linkAI:F4DataLinks = null;
         var aiNum:int = 0;
         var btn:BtnBase = param1;
         var btnStr:String = btn.name;
         if(btnStr == "back")
         {
            allRemoved = function():void
            {
               new UIMain(_sp,Attach.attach("UI_Main") as MovieClip,"UI_Main",_ui);
            };
            ConfigAndStory.saveData();
            this.removeSelf();
         }
         else if(btnStr == "help")
         {
            if(ConfigAndStory.STORY < 14)
            {
               new UIHelpX2(1,"Center",_sp,Attach.attach("UI_HelpChooseCraft") as MovieClip,uiName,_ui,this);
            }
            else
            {
               new UIHelpX2(2,"Center",_sp,Attach.attach("UI_HelpChooseCraftAI") as MovieClip,uiName,_ui,this);
            }
         }
         else if(btnStr == "factory")
         {
            allRemoved = function():void
            {
               new UIMakePlayer(ConfigAndStory.ALLPART,_sp,Attach.attach("UI_MakePlayer") as MovieClip,"UI_MakePlayer",_ui);
            };
            this.removeSelf();
         }
         else if(btnStr == "shop")
         {
            ConfigAndStory.SHOPNEW = false;
            allRemoved = function():void
            {
               new UIShop(ConfigAndStory.ALLPART,_sp,Attach.attach("UI_Shop") as MovieClip,"UI_Shop",_ui);
            };
            this.removeSelf();
         }
         else if(btnStr == "next")
         {
            this.form.next(true);
         }
         else if(btnStr == "prev")
         {
            this.form.next(false);
         }
         else if(btnStr == "first")
         {
            this.form.toPage(1);
         }
         else if(btnStr == "last")
         {
            this.form.toPage(this.form.allPage);
         }
         else if(btnStr == "mission")
         {
            new UIMission(_sp,Attach.attach("UI_Mission") as MovieClip,"UI_Mission",_ui,this);
         }
         else if(btnStr == "ready")
         {
            new UIReally(this.readyYes,null,null,"Ready?","Ready to fight?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
         }
         else if(btnStr.indexOf("choosesort") != -1)
         {
            sub = parseInt(btnStr.substr(10));
            this.form.sort(sub);
         }
         else if(btnStr.indexOf("form") != -1)
         {
            if(InitData.STAGE.mouseX < 520)
            {
               formNum = parseInt(btnStr.substr(4));
               f4link = this.form.getLink(formNum);
               if(f4link)
               {
                  if(f4link.isCaptainShip())
                  {
                     this.addDtL(f4link,this.dtLinkS,"showDetail",true);
                     this.form.lightOn(formNum);
                     InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                     return;
                  }
                  this.tempPlayerNum = formNum;
                  plNum = int(this.form.workArr[this.tempPlayerNum + this.form.page * this.form.pageLen][0]);
                  if(this.ckAIUseAI(plNum) == -1)
                  {
                     this.form.updatePlayer(this.tempPlayerNum);
                     sdata = new F4ShowData(f4link);
                     sdata.makeFlyClone();
                     this.addDtL(f4link,this.dtLinkF,"showFly",false);
                     InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                     this.addDtL(f4link,this.dtLinkS,"showDetail",true);
                     this.form.lightOn(formNum);
                  }
                  else
                  {
                     new UIReally(this.changeAI2PlayerYes,null,null,"Use AI\'s?","Really wanna pick this one?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
                  }
               }
            }
         }
         else if(btnStr == "bai")
         {
            parentStrAI = btn.parentName;
            formNumAI = parseInt(parentStrAI.substr(5));
            f4linkAI = this.form.getLink(formNumAI);
            if(f4linkAI)
            {
               if(f4linkAI.isCaptainShip())
               {
                  return;
               }
               this.addAI(f4linkAI.index);
               this.form.updateAI();
               this.addDtL(f4linkAI,this.dtLinkS,"showDetail",true);
            }
         }
         else if(btnStr.charAt(0) == "a")
         {
            aiNum = parseInt(btnStr.substr(1)) - 1;
            if(ConfigAndStory.AI[aiNum] == null)
            {
               this.reAI(aiNum);
            }
            else
            {
               this.delAI(aiNum);
            }
            this.form.updateAI();
         }
         else if(btnStr == "reset")
         {
            this.resetAI();
            this.form.updateAI();
         }
         else if(btnStr == "best")
         {
            this.fillBestAI();
            this.form.updateAI();
         }
      }
      
      public function changeAI2PlayerYes() : void
      {
         var _loc2_:F4ShowData = null;
         var _loc1_:F4DataLinks = this.form.getLink(this.tempPlayerNum);
         if(_loc1_)
         {
            this.form.updatePlayer(this.tempPlayerNum);
            _loc2_ = new F4ShowData(_loc1_);
            _loc2_.makeFlyClone();
            this.addDtL(_loc1_,this.dtLinkF,"showFly",false);
            this.findAIUse();
            this.fillAI();
            InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            this.form.updateAI();
            this.addDtL(_loc1_,this.dtLinkS,"showDetail",true);
            this.form.lightOn(this.tempPlayerNum);
         }
         this.tempPlayerNum = -1;
      }
      
      public function readyYes() : void
      {
         var tmpui:UIBase = null;
         var aiNums:int = 0;
         var i:int = 0;
         ConfigAndStory.saveData();
         ConfigAndStory.save();
         tmpui = this;
         allRemoved = function():void
         {
            ConfigAndStory.DIRECTOR.beforeLevelMov(playGame,tmpui);
         };
         InitData.CORE.repair.flys.length = 0;
         InitData.CORE.levelData.afa.length = 0;
         if(ConfigAndStory.noNext())
         {
            InitData.CORE.repair.flys.push(this.f4datax.plFly[ConfigAndStory.PLAYER]);
         }
         var _capFly:F4DataLinks = ConfigAndStory.getCaptainFly();
         if(_capFly)
         {
            InitData.CORE.repair.flys.push(_capFly);
         }
         if(ConfigAndStory.STORY != 49)
         {
            i = 0;
            while(i < ConfigAndStory.AI.length)
            {
               if(ConfigAndStory.AI[i] != null)
               {
                  aiNums++;
                  InitData.CORE.levelData.afa.push(this.f4datax.plFly[ConfigAndStory.AI[i]]);
                  InitData.CORE.repair.flys.push(this.f4datax.plFly[ConfigAndStory.AI[i]]);
               }
               i++;
            }
         }
         if(aiNums > 0)
         {
            MochiEvents.trackEvent("ready_ais",aiNums);
         }
         this.removeSelf();
      }
      
      public function playGame() : void
      {
         new UIMapBuild(_sp,Attach.attach("UI_MapBuilding") as MovieClip,"UI_MapBuilding",_ui);
      }
      
      public function ckPlayer() : F4DataLinks
      {
         return this.f4datax.plFly[ConfigAndStory.PLAYER];
      }
      
      public function addDtL(param1:F4DataLinks, param2:F4DataLinks, param3:String, param4:Boolean) : void
      {
         var _loc5_:MovieClip = null;
         var _loc6_:F4ShowData = null;
         if(param2 != param1)
         {
            _loc5_ = _mov.getChildByName(param3) as MovieClip;
            _loc6_ = new F4ShowData(param1,InitData.SELL);
            _loc6_.initData();
            _loc6_.makeFlyClone();
            ShowPart.addFly(_loc6_,_loc5_,param4);
            param2 = param1;
         }
      }
      
      public function cleanDtL(param1:String, param2:F4DataLinks, param3:Boolean) : void
      {
         ShowPart.cleanFly(_mov.getChildByName(param1) as MovieClip,param3);
         param2 = null;
      }
      
      public function ckAIUseAI(param1:int) : int
      {
         var _loc3_:int = 0;
         var _loc2_:Array = ConfigAndStory.AI;
         _loc3_ = 0;
         while(_loc3_ < this.ainum)
         {
            if(_loc2_[_loc3_] == param1)
            {
               return _loc3_;
            }
            _loc3_++;
         }
         return -1;
      }
      
      public function ckAIUse(param1:int) : Boolean
      {
         if(this.form && this.form.isCaptainShip(param1))
         {
            return true;
         }
         var _loc3_:int = 0;
         var _loc2_:Array = ConfigAndStory.AI;
         if(ConfigAndStory.PLAYER == param1)
         {
            return true;
         }
         _loc3_ = 0;
         while(_loc3_ < this.ainum)
         {
            if(_loc2_[_loc3_] == param1)
            {
               return true;
            }
            _loc3_++;
         }
         return false;
      }
      
      public function addAI(param1:int) : void
      {
         var _loc2_:F4DataLinks = null;
         if(this.f4datax && this.f4datax.plFly && param1 >= 0 && param1 < this.f4datax.plFly.length)
         {
            var _capCheck:F4DataLinks = this.f4datax.plFly[param1];
            if(_capCheck && _capCheck.isCaptainShip())
            {
               return;
            }
         }
         var _loc3_:F4ShowData = null;
         var _loc4_:Array = null;
         var _loc5_:int = 0;
         var _loc6_:Boolean = false;
         if(!this.ckAIUse(param1))
         {
            _loc2_ = this.f4datax.plFly[param1];
            _loc3_ = new F4ShowData(_loc2_);
            _loc3_.initData();
            _loc4_ = ConfigAndStory.AI;
            _loc6_ = false;
            _loc5_ = 0;
            while(_loc5_ < this.ainum)
            {
               if(_loc4_[_loc5_] == null)
               {
                  _loc4_[_loc5_] = param1;
                  var _aipad1:MovieClip = _mov.getChildByName("aipad") as MovieClip;
                  var _txtFld1:TextField = _aipad1 ? _aipad1.getChildByName("$a" + (_loc5_ + 1)) as TextField : null;
                  if(_txtFld1)
                  {
                     _txtFld1.text = _loc3_.cls;
                  }
                  this.ais[_loc5_] = param1;
                  InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
                  _loc6_ = true;
                  break;
               }
               _loc5_++;
            }
            if(!_loc6_)
            {
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[18]);
            }
         }
      }
      
      public function reAI(param1:int) : void
      {
         var _loc2_:int = 0;
         var _loc3_:F4DataLinks = null;
         var _loc4_:F4ShowData = null;
         var _loc5_:Array = null;
         if(this.ais[param1] != null)
         {
            _loc2_ = int(this.ais[param1]);
            if(!this.ckAIUse(_loc2_))
            {
               _loc3_ = this.f4datax.plFly[_loc2_];
               _loc4_ = new F4ShowData(_loc3_);
               _loc4_.initData();
               _loc5_ = ConfigAndStory.AI;
               _loc5_[param1] = _loc2_;
               var _aipad2:MovieClip = _mov.getChildByName("aipad") as MovieClip;
               var _txtFld2:TextField = _aipad2 ? _aipad2.getChildByName("$a" + (param1 + 1)) as TextField : null;
               if(_txtFld2)
               {
                  _txtFld2.text = _loc4_.cls;
               }
               this.addDtL(_loc3_,this.dtLinkS,"showDetail",true);
               InitData.SOUNDC2.playSoundOnce(InitData.SOUNDDATA.soundArray[14]);
            }
         }
      }
      
      public function delAI(param1:int) : void
      {
         var _loc2_:F4DataLinks = null;
         ConfigAndStory.AI[param1] = null;
         var _aipad3:MovieClip = _mov.getChildByName("aipad") as MovieClip;
         var _txtFld3:TextField = _aipad3 ? _aipad3.getChildByName("$a" + (param1 + 1)) as TextField : null;
         if(_txtFld3)
         {
            _txtFld3.text = "-";
         }
         if(this.ais[param1] != null)
         {
            _loc2_ = this.f4datax.plFly[this.ais[param1]];
            this.addDtL(_loc2_,this.dtLinkS,"showDetail",true);
         }
      }
      
      public function findAIUse() : void
      {
         var _loc2_:int = 0;
         var _loc1_:Array = ConfigAndStory.AI;
         _loc2_ = 0;
         while(_loc2_ < this.ainum)
         {
            if(_loc1_[_loc2_] != null && _loc1_[_loc2_] == ConfigAndStory.PLAYER)
            {
               _loc1_[_loc2_] = null;
            }
            _loc2_++;
         }
      }
      
      public function fillAI() : void
      {
         var _loc2_:F4DataLinks = null;
         var _loc4_:int = 0;
         var _loc1_:Array = ConfigAndStory.AI;
         var _loc3_:F4ShowData = new F4ShowData(_loc2_);
         var _aipad:MovieClip = _mov.getChildByName("aipad") as MovieClip;
         _loc4_ = 0;
         while(_loc4_ < this.ainum)
         {
            var _txtFld:TextField = _aipad ? _aipad.getChildByName("$a" + (_loc4_ + 1)) as TextField : null;
            if(_loc1_ != null && _loc4_ < _loc1_.length && _loc1_[_loc4_] != null && this.f4datax != null && this.f4datax.plFly != null)
            {
               _loc2_ = this.f4datax.plFly[_loc1_[_loc4_]];
               if(_loc2_ != null)
               {
                  _loc3_.link = _loc2_;
                  _loc3_.initData();
                  if(_txtFld != null)
                  {
                     _txtFld.text = _loc3_.cls;
                  }
                  this.ais[_loc4_] = _loc1_[_loc4_];
               }
               else
               {
                  if(_txtFld != null)
                  {
                     _txtFld.text = "-";
                  }
               }
            }
            else
            {
               if(_txtFld != null)
               {
                  _txtFld.text = "-";
               }
            }
            _loc4_++;
         }
      }
      
      public function resetAI() : void
      {
         var _loc1_:int = 0;
         _loc1_ = 0;
         while(_loc1_ < this.ainum)
         {
            this.delAI(_loc1_);
            _loc1_++;
         }
      }
      
      public function findBestAI() : int
      {
         var _loc2_:F4DataLinks = null;
         var _loc1_:F4ShowData = new F4ShowData(null);
         var _loc3_:int = 0;
         var _loc4_:int = -1;
         for each(_loc2_ in this.f4datax.plFly)
         {
            if(!this.ckAIUse(_loc2_.index))
            {
               _loc1_.link = _loc2_;
               _loc1_.initData();
               if(_loc1_.value > _loc3_)
               {
                  _loc4_ = _loc1_.link.index;
                  _loc3_ = _loc1_.value;
               }
            }
         }
         return _loc4_;
      }
      
      public function fillBestAI() : void
      {
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc1_:Array = ConfigAndStory.AI;
         _loc2_ = 0;
         while(_loc2_ < this.ainum)
         {
            if(_loc1_[_loc2_] == null)
            {
               _loc3_ = this.findBestAI();
               if(_loc3_ >= 0)
               {
                  this.addAI(_loc3_);
               }
            }
            _loc2_++;
         }
      }
   }
}

