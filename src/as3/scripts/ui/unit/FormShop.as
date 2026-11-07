package ui.unit
{
   import f4data.F4DataOrg;
   import f4data.sdata.ConfigAndStory;
   import flash.display.MovieClip;
   import flash.geom.ColorTransform;
   import flash.text.TextField;
   import ui.base.FormBase;
   
   public class FormShop extends FormBase
   {
      
      public var filx:int = 4;
      
      public var selNumA:int = 10;
      
      public var costMut:Number;

      public var isBuyShop:Boolean = false;
      
      public function FormShop(param1:MovieClip, param2:Array, param3:Number = 1)
      {
         var _loc4_:int = 0;
         line = 11;
         this.costMut = param3;
         super(param1,param2,5);
         this.filter();
         _loc4_ = 0;
         while(_loc4_ < pageLen)
         {
            (form.getChildByName("shopnew" + (_loc4_ + 1)) as MovieClip).mouseEnabled = false;
            (form.getChildByName("shopnew" + (_loc4_ + 1)) as MovieClip).mouseChildren = false;
            _loc4_++;
         }
      }
      
      override public function f4linkToArray(param1:*) : Array
      {
         var _loc2_:F4DataOrg = null;
         _loc2_ = param1;
         var _loc3_:Array = [];
         var _loc4_:Number = 0;
         if(_loc2_.firePoints.length > 0)
         {
            _loc4_ += _loc2_.firePoints.length * _loc2_.fire.firePowRate * _loc2_.bullet.bulletPow;
         }
         _loc4_ /= 300000;
         _loc3_.push(_loc2_.index);
         _loc3_.push(_loc2_.name);
         _loc3_.push(ConfigAndStory.num2Type(_loc2_.type));
         _loc3_.push(ConfigAndStory.num2LR(_loc2_.lr));
         _loc3_.push(_loc2_.maxhp);
         _loc3_.push((_loc2_.turnFly * (180 / Math.PI)).toFixed(1));
         if(_loc2_.type == F4DataOrg.TYPE_GUN)
         {
            _loc3_.push(int(_loc2_.getEffectiveRange()) + "");
         }
         else
         {
            _loc3_.push(_loc2_.powFly.toFixed(1));
         }
         _loc3_.push(_loc2_.mass);
         _loc3_.push(_loc4_);
         _loc3_.push(int(_loc2_.cost * this.costMut));
         _loc3_.push(_loc2_.sel);
         return _loc3_;
      }
      
      override public function updateForm1(param1:MovieClip, param2:Array) : void
      {
         (param1.getChildByName("index") as TextField).text = param2[0] + "";
         (param1.getChildByName("nameSelf") as TextField).text = param2[1];
         (param1.getChildByName("type") as TextField).text = param2[2] + "";
         (param1.getChildByName("lr") as TextField).text = param2[3] + "";
         (param1.getChildByName("hp") as TextField).text = param2[4] + "";
         (param1.getChildByName("turnfly") as TextField).text = param2[5] + "";
         (param1.getChildByName("powfly") as TextField).text = param2[6] + "";
         (param1.getChildByName("mass") as TextField).text = param2[7] + "";
         (param1.getChildByName("fire") as TextField).text = param2[8].toFixed(2) + "";
         (param1.getChildByName("value") as TextField).text = param2[9] + "";
         
         var _selCount:int = int(param2[10]);
         if(this.isBuyShop)
         {
            (param1.getChildByName("sel") as TextField).text = (_selCount <= 0 ? "N" : "x" + _selCount);
         }
         else
         {
            (param1.getChildByName("sel") as TextField).text = (_selCount <= 0 ? "N" : "Y");
         }
      }
      
      public function filter() : void
      {
         var _loc3_:F4DataOrg = null;
         var _loc1_:* = -1;
         var _loc2_:* = int(workArr.length);
         while(++_loc1_ < _loc2_)
         {
            _loc3_ = linkArr[workArr[_loc1_][0]];
            if(_loc3_.useAs != 0)
            {
               workArr.splice(_loc1_,1);
               _loc1_--;
               _loc2_--;
            }
         }
         allPage = Math.ceil(workArr.length / pageLen);
         if(allPage < 1)
         {
            allPage = 1;
         }
         if(page >= allPage)
         {
            page = allPage - 1;
         }
         this.updateForm();
      }
      
      public function filterX(param1:int) : void
      {
         var _loc2_:* = undefined;
         var _loc3_:* = 0;
         var _loc4_:* = 0;
         this.filx = param1;
         workArr.length = 0;
         for each(_loc2_ in linkArr)
         {
            workArr.push(this.f4linkToArray(_loc2_));
         }
         this.filter();
         if(param1 < 4)
         {
            _loc3_ = -1;
            _loc4_ = int(workArr.length);
            while(++_loc3_ < _loc4_)
            {
               if(workArr[_loc3_][2] != ConfigAndStory.num2Type(param1))
               {
                  workArr.splice(_loc3_,1);
                  _loc3_--;
                  _loc4_--;
               }
            }
            allPage = Math.ceil(workArr.length / pageLen);
            if(allPage < 1)
            {
               allPage = 1;
            }
            if(page >= allPage)
            {
               page = allPage - 1;
            }
            this.updateForm();
         }
         typeMC(param1);
      }
      
      public function getOrg(param1:int) : F4DataOrg
      {
         if(workArr[param1 + page * pageLen])
         {
            return linkArr[workArr[param1 + page * pageLen][0]];
         }
         return null;
      }
      
      override public function getLink(param1:int) : *
      {
         if(workArr[param1 + page * pageLen])
         {
            lastSort = -1;
            now = workArr[param1 + page * pageLen][0];
            var _itemRow:Array = workArr[param1 + page * pageLen];
            var _org:F4DataOrg = linkArr[_itemRow[0]] as F4DataOrg;
            
            if(this.isBuyShop)
            {
               if(_org && _org.type == 0)
               {
                  if(_itemRow[this.selNumA] == 0)
                  {
                     _itemRow[this.selNumA] = 1;
                  }
                  else
                  {
                     _itemRow[this.selNumA] = 0;
                  }
               }
               else
               {
                  var _curQty:int = int(_itemRow[this.selNumA]);
                  if(_curQty >= 99)
                  {
                     _itemRow[this.selNumA] = 0;
                  }
                  else
                  {
                     _itemRow[this.selNumA] = _curQty + 1;
                  }
               }
            }
            else
            {
               if(_itemRow[this.selNumA] == 0)
               {
                  _itemRow[this.selNumA] = 1;
               }
               else
               {
                  _itemRow[this.selNumA] = 0;
               }
            }
            if(_org)
            {
               _org.sel = _itemRow[this.selNumA];
            }
            this.updateForm();
            return _itemRow;
         }
         return null;
      }
      
      override public function updateForm() : void
      {
         var _loc1_:int = 0;
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            form.getChildByName("$form" + _loc1_).transform.colorTransform = new ColorTransform();
            cleanForm1(form.getChildByName("$form" + _loc1_) as MovieClip);
            _loc1_++;
         }
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            if(workArr[_loc1_ + pageLen * page])
            {
               if(workArr[_loc1_ + pageLen * page][this.selNumA] > 0)
               {
                  form.getChildByName("$form" + _loc1_).transform.colorTransform = new ColorTransform(1,1,1,1,80,80,80);
               }
               this.updateForm1(form.getChildByName("$form" + _loc1_) as MovieClip,workArr[_loc1_ + pageLen * page]);
            }
            _loc1_++;
         }
         (form.getChildByName("txtPage") as TextField).text = page + 1 + " / " + allPage;
         disableLight();
         this.updateNew();
      }
      
      public function delItemsShop() : void
      {
         var _loc3_:Array = null;
         var _loc5_:* = 0;
         var _loc6_:* = 0;
         var _loc1_:int = -1;
         var _loc2_:* = int(linkArr.length);
         var _loc4_:Array = [];
         for each(_loc3_ in workArr)
         {
            if(_loc3_[this.selNumA] > 0)
            {
               _loc4_.push(_loc3_);
            }
         }
         _loc5_ = -1;
         _loc6_ = int(_loc4_.length);
         loop1:
         while(++_loc5_ < _loc6_)
         {
            _loc1_ = -1;
            do
            {
               if(++_loc1_ >= _loc2_)
               {
                  continue loop1;
               }
            }
            while(_loc4_[_loc5_][0] != linkArr[_loc1_].index);
            
            var _shopOrg:F4DataOrg = linkArr[_loc1_];
            var _qty:int = int(_loc4_[_loc5_][this.selNumA]);
            
            if(_shopOrg && _shopOrg.type == 0)
            {
               ConfigAndStory.ALLPART.addPartFromShop(_loc1_);
               linkArr.splice(_loc1_, 1);
               _loc4_.splice(_loc5_, 1);
               _loc6_--;
               _loc5_--;
               _loc2_--;
            }
            else
            {
               var _k:int = 0;
               while(_k < _qty)
               {
                  ConfigAndStory.ALLPART.addPartFromShop(_loc1_);
                  _k++;
               }
               if(_loc1_ < linkArr.length && linkArr[_loc1_])
               {
                  linkArr[_loc1_].sel = 0;
               }
            }
         }
         ConfigAndStory.ALLPART.reSortIndex(linkArr);
         this.filterX(this.filx);
      }
      
      public function delItemsPlayer() : void
      {
         var _loc1_:int = -1;
         var _loc2_:int = int(workArr.length);
         while(++_loc1_ < _loc2_)
         {
            if(workArr[_loc1_][this.selNumA] == 1)
            {
               ConfigAndStory.ALLPART.addPartToShopFromPlayer(workArr[_loc1_][0]);
               delete linkArr[workArr[_loc1_][0]];
            }
         }
         this.filterX(this.filx);
      }
      
      public function selOne() : Boolean
      {
         var _loc1_:F4DataOrg = null;
         for each(_loc1_ in linkArr)
         {
            if(_loc1_ && _loc1_.sel > 0)
            {
               return true;
            }
         }
         return false;
      }
      
      public function selNum() : int
      {
         var _loc2_:F4DataOrg = null;
         var _loc1_:int = 0;
         for each(_loc2_ in linkArr)
         {
            if(_loc2_ && _loc2_.sel > 0)
            {
               _loc1_ += _loc2_.sel;
            }
         }
         return _loc1_;
      }
      
      public function reset() : void
      {
         var _loc1_:F4DataOrg = null;
         for each(_loc1_ in linkArr)
         {
            _loc1_.sel = 0;
         }
         this.filterX(this.filx);
      }
      
      public function allUnSel() : void
      {
         var _loc1_:F4DataOrg = null;
         for each(_loc1_ in linkArr)
         {
            _loc1_.sel = 0;
         }
         this.filterX(this.filx);
      }
      
      public function updateNew() : void
      {
         var _loc1_:int = 0;
         var _loc2_:F4DataOrg = null;
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            form.getChildByName("shopnew" + (_loc1_ + 1)).visible = false;
            _loc1_++;
         }
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            _loc2_ = this.getOrg(_loc1_);
            if(_loc2_)
            {
               if(_loc2_.snew)
               {
                  form.getChildByName("shopnew" + (_loc1_ + 1)).visible = true;
               }
            }
            _loc1_++;
         }
      }
      
      public function offNew() : void
      {
         var _loc1_:int = 0;
         _loc1_ = 0;
         while(_loc1_ < pageLen)
         {
            form.getChildByName("shopnew" + (_loc1_ + 1)).alpha = 0;
            _loc1_++;
         }
      }
      
      public function selInvert() : int
      {
         var _loc2_:Array = null;
         var _loc1_:int = 0;
         for each(_loc2_ in workArr)
         {
            if(_loc2_[this.selNumA] == 0)
            {
               _loc2_[this.selNumA] = 1;
               _loc1_ += _loc2_[9];
               (linkArr[_loc2_[0]] as F4DataOrg).sel = _loc2_[this.selNumA];
            }
            else
            {
               _loc2_[this.selNumA] = 0;
               _loc1_ -= _loc2_[9];
               (linkArr[_loc2_[0]] as F4DataOrg).sel = _loc2_[this.selNumA];
            }
         }
         this.updateForm();
         return _loc1_;
      }
   }
}

