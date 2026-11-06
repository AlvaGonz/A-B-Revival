package ui.unit
{
   import f4data.F4DataLinks;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import ui.base.FormBase;
   
   public class FormFly extends FormBase
   {
      
      public var selToDel:int = -1;
      
      public function FormFly(param1:MovieClip, param2:Array)
      {
         line = 2;
         super(param1,param2);
      }
      
      override public function f4linkToArray(param1:*) : Array
      {
         var _loc2_:F4DataLinks = param1;
         var _loc3_:Array = [];
         _loc3_.push(_loc2_.index);
         _loc3_.push(_loc2_.name);
         _loc3_.push(_loc2_.isSpecialProtected);
         return _loc3_;
      }
      
      override public function updateForm() : void
      {
         var _loc1_:int = 0;
         var _loc2_:MovieClip = null;
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
            var _item:Array = workArr[_loc1_ + pageLen * page];
            if(_item)
            {
               this.updateForm1(_loc2_,_item);
               _loc2_.getChildByName("$edit").visible = true;
               if(_item[2] == true)
               {
                  _loc2_.getChildByName("$break").visible = false;
               }
               else
               {
                  _loc2_.getChildByName("$break").visible = true;
               }
            }
            else
            {
               _loc2_.getChildByName("$edit").visible = false;
               _loc2_.getChildByName("$break").visible = false;
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
      }
      
      public function deleteNow() : void
      {
         var _loc1_:* = undefined;
         if(this.selToDel >= 0)
         {
            var _toDel:F4DataLinks = linkArr[this.selToDel] as F4DataLinks;
            if(_toDel && _toDel.isSpecialProtected)
            {
               return;
            }
            delete linkArr[this.selToDel];
            workArr.length = 0;
            for each(_loc1_ in linkArr)
            {
               workArr.push(this.f4linkToArray(_loc1_));
            }
            allPage = Math.ceil(workArr.length / pageLen);
            if(allPage < 1)
            {
               allPage = 1;
            }
            this.updateForm();
         }
      }
   }
}

