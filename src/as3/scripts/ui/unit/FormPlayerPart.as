package ui.unit
{
   import f4data.F4DataOrg;
   import f4data.sdata.ConfigAndStory;
   import flash.display.MovieClip;
   import flash.text.TextField;
   import ui.base.FormBase;
   
   public class FormPlayerPart extends FormBase
   {
      
      public var filx:int = 4;
      
      public function FormPlayerPart(param1:MovieClip, param2:Array)
      {
         line = 9;
         super(param1,param2);
         this.filter();
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
         updateForm();
      }
      
      public function pushOrg(param1:F4DataOrg) : void
      {
         workArr.push(this.f4linkToArray(param1));
         lastSort = -1;
      }
      
      public function updatePages() : void
      {
         allPage = Math.ceil(workArr.length / pageLen);
         if(allPage < 1)
         {
            allPage = 1;
         }
         updateForm();
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
            updateForm();
         }
         typeMC(param1);
      }
   }
}

