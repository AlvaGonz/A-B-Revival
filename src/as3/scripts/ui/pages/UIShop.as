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
   import ui.make.MakeUI;
   import ui.unit.FormShop;
   import ui.unit.ShowPart;
   
   public class UIShop extends UIBase
   {
      
      public var form1:FormShop;
      
      public var form2:FormShop;
      
      public var f4datax:F4DataCPX;
      
      public var tempMoney:int;
      
      public var dtOrg:F4DataOrg;
      
      public function UIShop(param1:F4DataCPX, param2:DisplayObjectContainer, param3:MovieClip, param4:String, param5:UICtrl, param6:UIBase = null)
      {
         this.f4datax = param1;
         this.form1 = new FormShop(param3.getChildByName("form1") as MovieClip,param1.shopPart);
         this.form1.isBuyShop = true;
         this.form2 = new FormShop(param3.getChildByName("form2") as MovieClip,param1.plPart,InitData.SELL);
         this.form2.isBuyShop = false;
         this.form1.initsortMC();
         this.form2.initsortMC();
         this.form2.offNew();
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
         var _loc3_:FormShop = null;
         var _loc2_:Boolean = _sp.mouseY < 240;
         if(_loc2_)
         {
            _loc3_ = this.form1;
         }
         else
         {
            _loc3_ = this.form2;
         }
         if(param1.indexOf("form") != -1)
         {
            _loc3_.disableLight();
         }
      }
      
      override public function overAct(param1:String) : void
      {
         var _loc3_:FormShop = null;
         var _loc4_:int = 0;
         var _loc5_:F4DataOrg = null;
         var _loc2_:Boolean = _sp.mouseY < 240;
         if(_loc2_)
         {
            _loc3_ = this.form1;
         }
         else
         {
            _loc3_ = this.form2;
         }
         if(param1.indexOf("form") != -1)
         {
            _loc4_ = parseInt(param1.substr(4));
            _loc5_ = _loc3_.getOrg(_loc4_);
            if(_loc5_)
            {
               if(_loc3_ == this.form1)
               {
                  this.addDt(_loc5_,1,1);
               }
               else
               {
                  this.addDt(_loc5_,2,InitData.SELL);
               }
               _loc3_.lightOn(_loc4_);
            }
         }
      }
      
      override public function afterFadeIn() : void
      {
         this.f4datax.reconcileShopStock();
         this.tempMoney = 0;
         this.updateTxt();
         this.updateAccept();
         this.form1.filterX(4);
         this.form1.initsortMC();
         this.form1.sort(9);
         this.form1.sort(9);
         this.form2.filterX(4);
         this.form2.initsortMC();
         this.form2.sort(9);
         if(ConfigAndStory.SHOWTEXTMENU[4] == 0)
         {
            ConfigAndStory.DIRECTOR.beforePlace(this.afterTalk,this,1,0,0);
            ConfigAndStory.SHOWTEXTMENU[4] = 1;
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
         if(ConfigAndStory.HOWTOPLAY[6] == 0)
         {
            new UIHelpX2(6,"Shop",_sp,Attach.attach("UI_HelpShop") as MovieClip,uiName,_ui,this);
         }
      }
      
      override public function removeSelf() : UIBase
      {
         super.removeSelf();
         return _last;
      }
      
      override public function act(param1:BtnBase) : void
      {
         var _loc4_:FormShop = null;
         var _loc5_:String = null;
         var _loc6_:int = 0;
         var _loc7_:int = 0;
         var _loc8_:int = 0;
         var _loc9_:Array = null;
         var _loc10_:int = 0;
         var _loc2_:String = param1.name;
         var _loc3_:Boolean = _sp.mouseY < 240;
         if(_loc3_)
         {
            _loc4_ = this.form1;
         }
         else
         {
            _loc4_ = this.form2;
         }
         if(_loc2_ == "accept")
         {
            if(this.form1.selOne() || this.form2.selOne())
            {
               if(ConfigAndStory.MONEY + this.tempMoney < 0)
               {
                  new UITalk(null,[377,377,null],_sp,Attach.attach("UI_Talk") as MovieClip,"UI_Talk",_ui,this);
               }
               else
               {
                  if(this.tempMoney >= 0)
                  {
                     _loc5_ = "You +" + this.tempMoney + "G\n Result: " + (ConfigAndStory.MONEY + this.tempMoney) + "G";
                  }
                  else
                  {
                     _loc5_ = "You " + this.tempMoney + "G\n Result: " + (ConfigAndStory.MONEY + this.tempMoney) + "G";
                  }
                  new UIReally(this.acceptYes,this.acceptNo,null,"Accept?",_loc5_,_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
               }
            }
         }
         else if(_loc2_ == "reset")
         {
            if(this.form1.selOne() || this.form2.selOne())
            {
               new UIReally(this.resetYes,this.resetNo,null,"Reset?","Cancel the deal?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
         }
         else if(_loc2_ == "invert")
         {
            _loc4_.selInvert();
            this.updateTotalCost();
            this.updateAccept();
         }
         else if(_loc2_ == "fly")
         {
            if(this.form1.selOne() || this.form2.selOne())
            {
               new UIReally(this.flyYes,this.flyNo,null,"Cancel?","Cancel the deal?",_sp,Attach.attach("UI_Really") as MovieClip,"UI_Really",_ui,this);
            }
            else
            {
               this.flyYes();
            }
         }
         else if(_loc2_ == "help")
         {
            new UIHelpX2(6,"Shop",_sp,Attach.attach("UI_HelpShop") as MovieClip,uiName,_ui,this);
         }
         if(_loc2_ == "next")
         {
            _loc4_.next(true);
         }
         else if(_loc2_ == "prev")
         {
            _loc4_.next(false);
         }
         else if(_loc2_ == "first")
         {
            _loc4_.toPage(1);
         }
         else if(_loc2_ == "last")
         {
            _loc4_.toPage(_loc4_.allPage);
         }
         else if(_loc2_.indexOf("choosesort") != -1)
         {
            _loc8_ = parseInt(_loc2_.substr(10));
            _loc4_.sort(_loc8_);
         }
         else if(_loc2_.indexOf("form") != -1)
         {
            _loc6_ = parseInt(_loc2_.substr(4));
            _loc9_ = _loc4_.getLink(_loc6_);
            if(_loc9_)
            {
               this.updateTotalCost();
               this.updateAccept();
            }
         }
         else if(_loc2_.indexOf("choosefil") != -1)
         {
            _loc6_ = parseInt(_loc2_.substr(9));
            _loc4_.lastSort = -1;
            _loc4_.sortMC();
            _loc4_.filterX(_loc6_);
            _loc4_.initsortMC();
            _loc4_.sort(9);
            if(_loc4_ == this.form1)
            {
               _loc4_.sort(9);
            }
         }
      }
      
      public function acceptYes() : void
      {
         var _loc1_:int = 0;
         MochiEvents.trackEvent("shop",this.tempMoney);
         this.form1.lastSort = -1;
         this.form1.sortMC();
         this.form1.filterX(4);
         this.form2.lastSort = -1;
         this.form2.sortMC();
         this.form2.filterX(4);
         this.form1.delItemsShop();
         this.form2.delItemsPlayer();
         ConfigAndStory.ALLPART.reSortIndex(this.form1.linkArr);
         this.form1.filterX(this.form1.filx);
         ConfigAndStory.MONEY += this.tempMoney;
         this.form1.reset();
         this.form2.reset();
         this.tempMoney = 0;
         (_mov.getChildByName("txtLast") as TextField).text = ConfigAndStory.MONEY + " G";
         this.updateTxt();
         this.cleanDt(1);
         this.cleanDt(2);
         this.form1.lastSort = -1;
         this.form2.lastSort = -1;
         this.form1.allUnSel();
         this.form2.allUnSel();
         this.updateAccept();
         this.form1.initsortMC();
         this.form1.sort(9);
         this.form1.sort(9);
         this.form2.initsortMC();
         this.form2.sort(9);
         if(ConfigAndStory.STORY < ConfigAndStory.MAP1)
         {
            _loc1_ = (ConfigAndStory.STORY + 1) * ConfigAndStory.STORY / 2 * 2007;
         }
         else
         {
            _loc1_ = (ConfigAndStory.MAP1 + 1) * ConfigAndStory.MAP1 / 2 * 2007;
         }
         if(ConfigAndStory.ALLPART.emptyPlPart() && ConfigAndStory.MONEY < _loc1_)
         {
            ConfigAndStory.MONEY = _loc1_;
            (_mov.getChildByName("txtLast") as TextField).text = ConfigAndStory.MONEY + " G";
            new UITalk(null,[386,387,null],_sp,Attach.attach("UI_Talk") as MovieClip,"UI_Talk",_ui,this);
         }
         else
         {
            new UITalk(null,[378,378,null],_sp,Attach.attach("UI_Talk") as MovieClip,"UI_Talk",_ui,this);
         }
      }
      
      public function updateAccept() : void
      {
         if(this.form1.selOne() || this.form2.selOne())
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
         this.form2.reset();
         this.tempMoney = 0;
         this.updateTxt();
         this.cleanDt(1);
         this.cleanDt(2);
         this.form1.lastSort = -1;
         this.form2.lastSort = -1;
         this.form1.allUnSel();
         this.form2.allUnSel();
         this.updateAccept();
         this.form1.initsortMC();
         this.form1.sort(9);
         this.form1.sort(9);
         this.form2.initsortMC();
         this.form2.sort(9);
      }
      
      public function resetNo() : void
      {
      }
      
      public function flyYes() : void
      {
         var tempui:UIBase = null;
         this.form1.allUnSel();
         this.form2.allUnSel();
         this.removeSelf();
         tempui = this;
         allRemoved = function():void
         {
            MakeUI.makeChoose(tempui,_sp,_ui);
         };
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
      
      public function updateTotalCost() : void
      {
         var _total:int = 0;
         var _item:Array = null;
         for each(_item in this.form1.workArr)
         {
            if(_item && int(_item[this.form1.selNumA]) > 0)
            {
               _total -= int(_item[9]) * int(_item[this.form1.selNumA]);
            }
         }
         for each(_item in this.form2.workArr)
         {
            if(_item && int(_item[this.form2.selNumA]) > 0)
            {
               _total += int(_item[9]);
            }
         }
         this.tempMoney = _total;
         this.updateTxt();
      }
      
      public function addDt(param1:F4DataOrg, param2:int, param3:Number) : void
      {
         var _loc4_:MovieClip = null;
         if(this.dtOrg != param1)
         {
            _loc4_ = _mov.getChildByName("dt" + param2) as MovieClip;
            ShowPart.addPart(param1,_loc4_,param3);
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

