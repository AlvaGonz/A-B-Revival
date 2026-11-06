package ui.pages
{
   import f4.data.InitData;
   import f4data.F4DataOrg;
   import f4data.cpx.F4DataCPX;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObjectContainer;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import fullshit.tool.Attach;
   import mochi.as3.MochiEvents;
   import ui.UICtrl;
   import ui.base.BtnBase;
   import ui.base.UIBase;
   import ui.unit.FormRepair;
   import ui.unit.ShowPart;
   
   public class UIRepair extends UIBase
   {
      
      public var form1:FormRepair;
      
      public var f4datax:F4DataCPX;
      
      public var tempMoney:int;
      
      public var dtOrg:F4DataOrg;
      
      public function UIRepair(param1:F4DataCPX, param2:DisplayObjectContainer, param3:MovieClip, param4:String, param5:UICtrl, param6:UIBase = null)
      {
         this.f4datax = param1;
         this.form1 = new FormRepair(param3.getChildByName("form1") as MovieClip,param1.repairPart,InitData.REPAIR);
         this.form1.initsortMC();
         (param3.getChildByName("txtLast") as TextField).text = ConfigAndStory.MONEY + " G";
         super(param2,param3,param4,param5,param6);
      }
      
      public function wordColor(param1:Boolean) : void
      {
         if(param1)
         {
            (_mov.getChildByName("txtNow") as TextField).textColor = 13369344;
         }
         else
         {
            (_mov.getChildByName("txtNow") as TextField).textColor = 13421534;
         }
      }
      
      override public function outAct(param1:String) : void
      {
         if(param1.indexOf("form") != -1)
         {
            this.form1.disableLight();
         }
      }
      
      override public function overAct(param1:String) : void
      {
         var _loc2_:FormRepair = null;
         var _loc3_:int = 0;
         var _loc4_:F4DataOrg = null;
         _loc2_ = this.form1;
         if(param1.indexOf("form") != -1)
         {
            _loc3_ = parseInt(param1.substr(4));
            _loc4_ = _loc2_.getOrg(_loc3_);
            if(_loc4_)
            {
               this.addDt(_loc4_,1);
               this.form1.lightOn(_loc3_);
            }
         }
      }
      
      override public function afterFadeIn() : void
      {
         this.tempMoney = 0;
         this.updateAccept();
         this.form1.filterX(4);
         this.form1.initsortMC();
         this.form1.sort(0);
         this.form1.sort(0);
         if(ConfigAndStory.SHOWTEXTMENU[3] == 0)
         {
            ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,3,0,0);
            ConfigAndStory.SHOWTEXTMENU[3] = 1;
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
         if(ConfigAndStory.HOWTOPLAY[5] == 0)
         {
            new UIHelpX2(5,"Repair",_sp,Attach.attach("UI_HelpRepair") as MovieClip,uiName,_ui,this);
         }
      }
      
      override public function removeSelf() : UIBase
      {
         super.removeSelf();
         return _last;
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc3_:FormRepair = null;
         var _loc4_:String = null;
         var _loc5_:int = 0;
         var _loc6_:int = 0;
         var _loc7_:int = 0;
         var _loc8_:Array = null;
         var _loc9_:int = 0;
         var _loc2_:String = param1.name;
         _loc3_ = this.form1;
         if(_loc2_ == "accept")
         {
            if(this.form1.selOne())
            {
               if(ConfigAndStory.MONEY + this.tempMoney < 0)
               {
                  new UITalk(null,[381,381,null],_sp,Attach.attach("UI_Talk") as MovieClip,"UI_Talk",_ui,this);
               }
               else
               {
                  if(this.tempMoney >= 0)
                  {
                     _loc4_ = "You +" + this.tempMoney + "G\n Result: " + (ConfigAndStory.MONEY + this.tempMoney) + "G";
                  }
                  else
                  {
                     _loc4_ = "You " + this.tempMoney + "G\n Result: " + (ConfigAndStory.MONEY + this.tempMoney) + "G";
                  }
                  new UIReally(this.acceptYes,this.acceptNo,null,"Accept?",_loc4_,_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
               }
            }
         }
         else if(_loc2_ == "help")
         {
            new UIHelpX2(5,"Repair",_sp,Attach.attach("UI_HelpRepair") as MovieClip,uiName,_ui,this);
         }
         else if(_loc2_ == "all")
         {
            _loc6_ = _loc3_.selAll();
            if(_loc6_ != 0)
            {
               this.tempMoney -= _loc6_;
               this.updateTxt();
            }
            this.updateAccept();
         }
         else if(_loc2_ == "reset")
         {
            if(this.form1.selOne())
            {
               new UIReally(this.resetYes,this.resetNo,null,"Reset?","Cancel the deal?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         else if(_loc2_ == "exit")
         {
            if(this.form1.selOne())
            {
               new UIReally(this.flyYes,this.flyNo,null,"Cancel?","Cancel the deal?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
            else
            {
               this.flyYes();
            }
         }
         if(_loc2_ == "next")
         {
            _loc3_.next(true);
         }
         else if(_loc2_ == "prev")
         {
            _loc3_.next(false);
         }
         else if(_loc2_ == "first")
         {
            _loc3_.toPage(1);
         }
         else if(_loc2_ == "last")
         {
            _loc3_.toPage(_loc3_.allPage);
         }
         else if(_loc2_.indexOf("choosesort") != -1)
         {
            _loc7_ = parseInt(_loc2_.substr(10));
            _loc3_.sort(_loc7_);
         }
         else if(_loc2_.indexOf("form") != -1)
         {
            _loc5_ = parseInt(_loc2_.substr(4));
            _loc8_ = _loc3_.getLink(_loc5_);
            if(_loc8_)
            {
               if(_loc8_[10] == 1)
               {
                  _loc9_ = int(_loc8_[9]);
               }
               else
               {
                  _loc9_ = -_loc8_[9];
               }
               if(_loc9_ != 0)
               {
                  if(_loc3_ == this.form1)
                  {
                     this.tempMoney -= _loc9_;
                  }
                  else
                  {
                     this.tempMoney += _loc9_;
                  }
                  this.updateTxt();
               }
               this.updateAccept();
            }
         }
         else if(_loc2_.indexOf("choosefil") != -1)
         {
            _loc5_ = parseInt(_loc2_.substr(9));
            _loc3_.lastSort = -1;
            _loc3_.sortMC();
            _loc3_.filterX(_loc5_);
            _loc3_.initsortMC();
            _loc3_.sort(0);
            _loc3_.sort(0);
         }
      }
      
      public function acceptYes() : void
      {
         MochiEvents.trackEvent("repair",this.tempMoney);
         this.form1.lastSort = -1;
         this.form1.sortMC();
         this.form1.filterX(4);
         this.form1.delItemsShop();
         ConfigAndStory.MONEY += this.tempMoney;
         this.form1.reset();
         this.tempMoney = 0;
         (_mov.getChildByName("txtLast") as TextField).text = ConfigAndStory.MONEY + " G";
         this.updateTxt();
         this.cleanDt(1);
         this.form1.lastSort = -1;
         this.form1.allUnSel();
         this.updateAccept();
         this.form1.init();
         this.form1.filterX(4);
         this.form1.initsortMC();
         this.form1.sort(0);
         this.form1.sort(0);
         new UITalk(null,[378,378,null],_sp,Attach.attach("UI_Talk") as MovieClip,"UI_Talk",_ui,this);
      }
      
      public function updateAccept() : void
      {
         if(this.form1.selOne())
         {
            enableBtn("accept");
            enableBtn("reset");
         }
         else
         {
            disableBtn("accept");
            disableBtn("reset");
         }
      }
      
      public function acceptNo() : void
      {
      }
      
      public function resetYes() : void
      {
         this.form1.reset();
         this.tempMoney = 0;
         this.updateTxt();
         this.cleanDt(1);
         this.form1.lastSort = -1;
         this.form1.allUnSel();
         this.updateAccept();
         this.form1.init();
         this.form1.filterX(4);
         this.form1.initsortMC();
         this.form1.sort(0);
         this.form1.sort(0);
      }
      
      public function resetNo() : void
      {
      }
      
      public function flyYes() : void
      {
         this.form1.allUnSel();
         this.removeSelf();
      }
      
      public function flyNo() : void
      {
      }
      
      public function updateTxt() : void
      {
         this.wordColor(ConfigAndStory.MONEY + this.tempMoney < 0);
         if(this.tempMoney > 0)
         {
            (_mov.getChildByName("txtNow") as TextField).text = "+" + this.tempMoney + " G";
         }
         else
         {
            (_mov.getChildByName("txtNow") as TextField).text = this.tempMoney + " G";
         }
      }
      
      public function addDt(param1:F4DataOrg, param2:int) : void
      {
         var _loc3_:MovieClip = null;
         if(this.dtOrg != param1)
         {
            _loc3_ = _mov.getChildByName("dt" + param2) as MovieClip;
            ShowPart.addPart(param1,_loc3_,InitData.REPAIR);
            this.dtOrg = param1;
         }
      }
      
      public function cleanDt(param1:int) : void
      {
         ShowPart.cleanDt(_mov.getChildByName("dt" + param1) as MovieClip);
         this.dtOrg = null;
      }
   }
}

