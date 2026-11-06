package ui.base
{
   import f4data.F4DataLinks;
   import f4data.cpx.QuickSort;
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.text.TextField;
   
   public class FormBase
   {
      
      public var form:MovieClip;
      
      public var workArr:Array;
      
      public var linkArr:Array;
      
      public var page:int;
      
      public var allPage:int;
      
      public var pageLen:int;
      
      public var now:int = -1;
      
      public var qs:QuickSort;
      
      public var lastSort:int = -1;
      
      public var line:int;
      
      public function FormBase(param1:MovieClip, param2:Array, param3:int = 10)
      {
         super();
         this.form = param1;
         this.linkArr = param2;
         this.workArr = [];
         this.page = 0;
         this.pageLen = param3;
         this.qs = new QuickSort();
         this.init();
         this.disableLight();
      }
      
      public function changeTextColor(param1:int, param2:Boolean) : void
      {
         var _loc5_:DisplayObject = null;
         var _loc3_:MovieClip = this.form.getChildByName("$form" + param1) as MovieClip;
         var _loc4_:int = _loc3_.numChildren;
         var _loc6_:int = 0;
         while(_loc6_ < _loc4_)
         {
            _loc5_ = _loc3_.getChildAt(_loc6_);
            if(_loc5_ is TextField)
            {
               if(param2)
               {
                  (_loc5_ as TextField).textColor = 16777215;
               }
               else
               {
                  (_loc5_ as TextField).textColor = 6778750;
               }
            }
            _loc6_++;
         }
      }
      
      public function disableLight() : void
      {
         var _loc1_:int = 0;
         _loc1_ = 0;
         while(_loc1_ < this.pageLen)
         {
            (this.form.getChildByName("$form" + _loc1_) as MovieClip).getChildByName("light").visible = false;
            this.changeTextColor(_loc1_,false);
            _loc1_++;
         }
      }
      
      public function lightOn(param1:int) : void
      {
         this.disableLight();
         (this.form.getChildByName("$form" + param1) as MovieClip).getChildByName("light").visible = true;
         this.changeTextColor(param1,true);
      }
      
      public function init() : void
      {
         var _loc1_:* = undefined;
         this.workArr = [];
         for each(_loc1_ in this.linkArr)
         {
            this.workArr.push(this.f4linkToArray(_loc1_));
         }
         this.allPage = Math.ceil(this.workArr.length / this.pageLen);
         if(this.allPage < 1)
         {
            this.allPage = 1;
         }
         this.updateForm();
      }
      
      public function f4linkToArray(param1:*) : Array
      {
         return null;
      }
      
      public function cleanForm1(param1:MovieClip) : void
      {
         var _loc2_:DisplayObject = null;
         var _loc3_:int = 0;
         while(_loc3_ < param1.numChildren)
         {
            _loc2_ = param1.getChildAt(_loc3_);
            if(_loc2_ is TextField)
            {
               (_loc2_ as TextField).text = "";
            }
            _loc3_++;
         }
      }
      
      public function updateForm1(param1:MovieClip, param2:Array) : void
      {
         (param1.getChildByName("index") as TextField).text = param2[0] + "";
         (param1.getChildByName("nameSelf") as TextField).text = param2[1];
      }
      
      public function updateForm() : void
      {
         var _loc1_:int = 0;
         _loc1_ = 0;
         while(_loc1_ < this.pageLen)
         {
            this.cleanForm1(this.form.getChildByName("$form" + _loc1_) as MovieClip);
            _loc1_++;
         }
         _loc1_ = 0;
         while(_loc1_ < this.pageLen)
         {
            if(this.workArr[_loc1_ + this.pageLen * this.page])
            {
               this.updateForm1(this.form.getChildByName("$form" + _loc1_) as MovieClip,this.workArr[_loc1_ + this.pageLen * this.page]);
            }
            _loc1_++;
         }
         (this.form.getChildByName("txtPage") as TextField).text = this.page + 1 + " / " + this.allPage;
         this.disableLight();
      }
      
      public function next(param1:Boolean) : void
      {
         if(param1)
         {
            ++this.page;
            if(this.page >= this.allPage)
            {
               this.page = 0;
            }
         }
         else
         {
            --this.page;
            if(this.page < 0)
            {
               this.page = this.allPage - 1;
            }
         }
         this.updateForm();
      }
      
      public function toPage(param1:int) : void
      {
         this.page = param1 - 1;
         if(param1 < 0)
         {
            param1 = 0;
         }
         if(param1 >= this.allPage)
         {
            param1 = this.allPage - 1;
         }
         this.updateForm();
      }
      
      public function sort(param1:int) : void
      {
         if(this.lastSort == param1)
         {
            this.backSort();
         }
         else
         {
            this.lastSort = param1;
            this.qs.sort(this.workArr,param1);
         }
         this.sortMC();
         this.updateForm();
      }
      
      public function backSort() : void
      {
         var _loc3_:* = undefined;
         var _loc1_:int = this.workArr.length / 2;
         var _loc2_:int = int(this.workArr.length);
         var _loc4_:int = 0;
         while(_loc4_ < _loc1_)
         {
            _loc3_ = this.workArr[_loc4_];
            this.workArr[_loc4_] = this.workArr[_loc2_ - 1 - _loc4_];
            this.workArr[_loc2_ - 1 - _loc4_] = _loc3_;
            _loc4_++;
         }
      }
      
      public function getLink(param1:int) : *
      {
         if(this.workArr[param1 + this.page * this.pageLen])
         {
            this.now = this.workArr[param1 + this.page * this.pageLen][0];
            return this.linkArr[this.now];
         }
         return null;
      }
      
      public function removeNow() : void
      {
         var _loc1_:Array = this.findInWA(this.now);
         this.linkArr.splice(this.now,1);
         this.workArr.splice(this.workArr.indexOf(_loc1_),1);
         this.makeIndex();
         this.allPage = Math.ceil(this.workArr.length / this.pageLen);
         if(this.allPage < 1)
         {
            this.allPage = 1;
         }
         this.updateForm();
      }
      
      public function makeIndex() : void
      {
         var _loc1_:int = 0;
         var _loc2_:int = 0;
         _loc2_ = int(this.linkArr.length);
         _loc1_ = 0;
         while(_loc1_ < _loc2_)
         {
            this.findInWA((this.linkArr[_loc1_] as F4DataLinks).index)[0] = _loc1_;
            (this.linkArr[_loc1_] as F4DataLinks).index = _loc1_;
            _loc1_++;
         }
      }
      
      public function findInWA(param1:int) : Array
      {
         var _loc2_:Array = null;
         for each(_loc2_ in this.workArr)
         {
            if(_loc2_[0] == param1)
            {
               return _loc2_;
            }
         }
         return null;
      }
      
      public function sortMC() : void
      {
         var _loc1_:MovieClip = null;
         var _loc2_:int = 0;
         while(_loc2_ < this.line)
         {
            _loc1_ = this.form.getChildByName("$choosesort" + _loc2_) as MovieClip;
            if(_loc2_ == this.lastSort)
            {
               if(_loc1_.currentFrame == 1)
               {
                  _loc1_.gotoAndStop(2);
               }
               else
               {
                  _loc1_.gotoAndStop(1);
               }
            }
            else
            {
               _loc1_.gotoAndStop(3);
            }
            _loc2_++;
         }
      }
      
      public function initsortMC() : void
      {
         var _loc1_:MovieClip = null;
         var _loc2_:int = 0;
         while(_loc2_ < this.line)
         {
            _loc1_ = this.form.getChildByName("$choosesort" + _loc2_) as MovieClip;
            _loc1_.gotoAndStop(3);
            _loc2_++;
         }
      }
      
      public function typeMC(param1:int) : void
      {
         var _loc2_:MovieClip = null;
         var _loc3_:int = 0;
         while(_loc3_ < 5)
         {
            _loc2_ = this.form.getChildByName("$choosefil" + _loc3_) as MovieClip;
            if(_loc3_ == param1)
            {
               _loc2_.gotoAndStop(2);
            }
            else
            {
               _loc2_.gotoAndStop(1);
            }
            _loc3_++;
         }
      }
   }
}

