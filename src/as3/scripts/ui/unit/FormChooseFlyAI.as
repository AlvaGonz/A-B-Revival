package ui.unit
{
   import f4data.F4DataLinks;
   import f4data.sdata.ConfigAndStory;
   import flash.display.MovieClip;
   import flash.filters.GlowFilter;
   import flash.text.TextField;
   import flash.text.TextFormat;
   import flash.text.TextFormatAlign;
   import ui.base.FormBase;
   import ui.base.UIStyles;
   
   public class FormChooseFlyAI extends FormBase
   {
      
      public function FormChooseFlyAI(param1:MovieClip, param2:Array)
      {
         line = 4;
         super(param1,param2);
      }
      
      public function isCaptainShip(param1:int) : Boolean
      {
         if(linkArr && param1 >= 0 && param1 < linkArr.length && linkArr[param1])
         {
            var _loc2_:F4DataLinks = linkArr[param1] as F4DataLinks;
            if(_loc2_)
            {
               return _loc2_.isCaptainShip();
            }
         }
         return false;
      }
      
      override public function f4linkToArray(param1:*) : Array
      {
         var _loc2_:F4DataLinks = param1;
         var _loc3_:Array = [];
         _loc3_.push(_loc2_.index);
         _loc3_.push(_loc2_.name);
         if(_loc2_.index == ConfigAndStory.PLAYER)
         {
            _loc3_.push(1);
         }
         else
         {
            _loc3_.push(0);
         }
         if(ConfigAndStory.AI.indexOf(_loc2_.index) != -1)
         {
            _loc3_.push(1);
         }
         else
         {
            _loc3_.push(0);
         }
         return _loc3_;
      }
      
      public function ckAIShow(param1:int) : Boolean
      {
         if(this.isCaptainShip(param1))
         {
            return false;
         }
         var _loc2_:int = 0;
         while(_loc2_ < ConfigAndStory.AI.length)
         {
            if(ConfigAndStory.AI[_loc2_] == param1)
            {
               return false;
            }
            _loc2_++;
         }
         if(ConfigAndStory.PLAYER == param1)
         {
            return false;
         }
         return true;
      }
      
      override public function updateForm() : void
      {
         var _loc1_:int = 0;
         var _loc2_:MovieClip = null;
         var _loc3_:int = 0;
         var _badge:TextField = null;
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            cleanForm1(form.getChildByName("$form" + _loc1_) as MovieClip);
            _loc1_++;
         }
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            _loc2_ = form.getChildByName("$form" + _loc1_) as MovieClip;
            if(workArr[_loc1_ + pageLen * page])
            {
               this.updateForm1(_loc2_,workArr[_loc1_ + pageLen * page]);
               _loc3_ = int(workArr[_loc1_ + pageLen * page][0]);
               _badge = _loc2_.getChildByName("txtCaptainBadge") as TextField;
               if(this.isCaptainShip(_loc3_))
               {
                  _loc2_.getChildByName("$bai").visible = false;
                  if(!_badge)
                  {
                     _badge = new TextField();
                     _badge.name = "txtCaptainBadge";
                     UIStyles.applyFormat(_badge, UIStyles.FONT_BODY, UIStyles.SIZE_SMALL, UIStyles.COLOR_SYSTEM_LIGHT, true, TextFormatAlign.CENTER);
                     _badge.text = "[CAPTAIN]";
                     var _bai:MovieClip = _loc2_.getChildByName("$bai") as MovieClip;
                     if(_bai)
                     {
                        _badge.x = _bai.x;
                        _badge.y = _bai.y + 2;
                        _badge.width = _bai.width;
                        _badge.height = _bai.height;
                     }
                     _badge.selectable = false;
                     _badge.mouseEnabled = false;
                     UIStyles.applyGlow(_badge, 0x003366, 0.8, 3, 2);
                     _loc2_.addChild(_badge);
                  }
                  _badge.visible = true;
                  (_loc2_.getChildByName("player") as TextField).text = "-";
                  (_loc2_.getChildByName("ai") as TextField).text = "ALLY";
               }
               else
               {
                  if(_badge)
                  {
                     _badge.visible = false;
                  }
                  if(this.ckAIShow(_loc3_))
                  {
                     _loc2_.getChildByName("$bai").visible = true;
                  }
                  else
                  {
                     _loc2_.getChildByName("$bai").visible = false;
                  }
               }
            }
            else
            {
               _loc2_.getChildByName("$bai").visible = false;
               _badge = _loc2_.getChildByName("txtCaptainBadge") as TextField;
               if(_badge)
               {
                  _badge.visible = false;
               }
            }
            _loc1_++;
         }
         (form.getChildByName("txtPage") as TextField).text = page + 1 + " / " + allPage;
         disableLight();
      }
      
      override public function updateForm1(param1:MovieClip, param2:Array) : void
      {
         (param1.getChildByName("index") as TextField).text = param2[0] + "";
         (param1.getChildByName("nameSelf") as TextField).text = param2[1];
         (param1.getChildByName("player") as TextField).text = param2[2] + "";
         (param1.getChildByName("ai") as TextField).text = param2[3] + "";
      }
      
      public function updateAI() : void
      {
         var _loc1_:Array = ConfigAndStory.AI;
         var _loc2_:int = 0;
         var _loc3_:int = 0;
         var _loc4_:int = 20;
         var _loc5_:int = int(workArr.length);
         _loc2_ = 0;
         while(_loc2_ < _loc5_)
         {
            if(_loc1_.indexOf(workArr[_loc2_][0]) == -1)
            {
               workArr[_loc2_][3] = 0;
            }
            else
            {
               _loc3_ = 0;
               while(_loc3_ < _loc4_)
               {
                  if(workArr[_loc2_][0] == _loc1_[_loc3_])
                  {
                     workArr[_loc2_][3] = _loc3_ + 1;
                     break;
                  }
                  _loc3_++;
               }
            }
            _loc2_++;
         }
         this.updateForm();
      }
      
      public function updatePlayer(param1:int) : void
      {
         if(workArr[param1 + page * pageLen])
         {
            ConfigAndStory.PLAYER = workArr[param1 + page * pageLen][0];
            workArr[param1 + page * pageLen][2] = 1;
         }
         var _loc2_:int = 0;
         while(_loc2_ < workArr.length)
         {
            if(workArr[_loc2_][2] == 1 && ConfigAndStory.PLAYER != workArr[_loc2_][0])
            {
               workArr[_loc2_][2] = 0;
            }
            _loc2_++;
         }
         this.updateForm();
      }
   }
}

