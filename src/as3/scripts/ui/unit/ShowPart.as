package ui.unit
{
   import f4data.F4DataOrg;
   import f4data.F4ShowData;
   import f4data.sdata.ConfigAndStory;
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.display.Sprite;
   import flash.text.TextField;
   import fullshit.tool.Attach;
   
   public class ShowPart
   {
      
      public function ShowPart()
      {
         super();
      }
      
      public static function addPart(param1:F4DataOrg, param2:MovieClip, param3:Number) : void
      {
         var _loc11_:int = 0;
         var _loc13_:DisplayObject = null;
         var _loc14_:String = null;
         var _loc4_:MovieClip = param2.getChildByName("pic") as MovieClip;
         var _loc5_:MovieClip = Attach.attach(param1.mcName) as MovieClip;
         _loc5_.name = "partx";
         var _loc6_:DisplayObject = _loc4_.getChildByName("partx");
         if(_loc6_)
         {
            _loc4_.removeChild(_loc6_);
         }
         var _loc7_:Number = _loc4_.width / _loc5_.height;
         var _loc8_:Number = _loc4_.height / _loc5_.width;
         var _loc9_:Number = Math.min(_loc7_,_loc8_);
         _loc5_.scaleX = _loc5_.scaleY = _loc9_;
         if(param1.lr == 1)
         {
            _loc5_.scaleY = -_loc5_.scaleY;
         }
         _loc5_.y = _loc5_.width / 2;
         _loc5_.x = _loc5_.height / 2;
         _loc5_.rotation = -90;
         var _loc10_:MovieClip = _loc5_.getChildByName("SHOW") as MovieClip;
         var _loc12_:int = _loc10_.numChildren;
         _loc11_ = 0;
         while(_loc11_ < _loc12_)
         {
            _loc13_ = _loc10_.getChildAt(_loc11_);
            _loc14_ = _loc13_.name;
            if(_loc14_.charAt(0) == "L")
            {
               _loc13_.visible = false;
            }
            else if(_loc14_.charAt(0) == "F")
            {
               _loc13_.visible = false;
            }
            else if(_loc14_.charAt(0) == "P")
            {
               _loc13_.visible = false;
            }
            _loc11_++;
         }
         var _loc15_:Number = 0;
         if(param1.firePoints.length > 0)
         {
            _loc15_ += param1.firePoints.length * param1.fire.firePowRate * param1.bullet.bulletPow;
         }
         _loc15_ /= 300000;
         (param2.getChildByName("value") as TextField).text = int(param1.cost * param3) + "";
         (param2.getChildByName("type") as TextField).text = ConfigAndStory.num2Type(param1.type) + "";
         (param2.getChildByName("nameSelf") as TextField).text = param1.name;
         (param2.getChildByName("hp") as TextField).text = param1.maxhp + "";
         (param2.getChildByName("fire") as TextField).text = _loc15_.toFixed(2);
         (param2.getChildByName("turn") as TextField).text = (param1.turnFly * (180 / Math.PI)).toFixed(1);
         (param2.getChildByName("pow") as TextField).text = param1.powFly.toFixed(1);
         (param2.getChildByName("mass") as TextField).text = param1.mass + "";
         _loc4_.addChild(_loc5_);
      }
      
      public static function cleanDt(param1:MovieClip) : void
      {
         var _loc2_:MovieClip = param1.getChildByName("pic") as MovieClip;
         _loc2_ = _loc2_.parent as MovieClip;
         var _loc3_:DisplayObject = _loc2_.getChildByName("partx");
         if(_loc3_)
         {
            _loc2_.removeChild(_loc3_);
         }
         (param1.getChildByName("value") as TextField).text = "-";
         (param1.getChildByName("type") as TextField).text = "-";
         (param1.getChildByName("nameSelf") as TextField).text = "-";
         (param1.getChildByName("hp") as TextField).text = "-";
         (param1.getChildByName("fire") as TextField).text = "-";
         (param1.getChildByName("turn") as TextField).text = "-";
         (param1.getChildByName("pow") as TextField).text = "-";
         (param1.getChildByName("mass") as TextField).text = "-";
      }
      
      public static function cleanFly(param1:MovieClip, param2:Boolean = true) : void
      {
         var _loc3_:MovieClip = param1.getChildByName("pic") as MovieClip;
         var _loc4_:DisplayObject = _loc3_.parent.getChildByName("partx");
         if(_loc4_)
         {
            _loc3_.parent.removeChild(_loc4_);
         }
         if(param2)
         {
            (param1.getChildByName("txtName") as TextField).text = "";
            (param1.getChildByName("txtClass") as TextField).text = "";
            (param1.getChildByName("txtCpx") as TextField).text = "";
            (param1.getChildByName("txtValue") as TextField).text = "";
            (param1.getChildByName("txtHp") as TextField).text = "";
            (param1.getChildByName("txtFire") as TextField).text = "";
            (param1.getChildByName("txtSpeed") as TextField).text = "";
            (param1.getChildByName("txtTurn") as TextField).text = "";
            param1.getChildByName("inuse").alpha = 0;
         }
      }
      
      public static function addFly(param1:F4ShowData, param2:MovieClip, param3:Boolean = true) : void
      {
         var _loc10_:Boolean = false;
         var _loc11_:int = 0;
         var _loc4_:MovieClip = param2.getChildByName("pic") as MovieClip;
         var _loc5_:Sprite = param1.sp;
         _loc5_.name = "partx";
         var _loc6_:DisplayObject = _loc4_.parent.getChildByName("partx");
         if(_loc6_)
         {
            _loc4_.parent.removeChild(_loc6_);
         }
         var _loc7_:Number = _loc4_.width / _loc5_.height;
         var _loc8_:Number = _loc4_.height / _loc5_.width;
         var _loc9_:Number = Math.min(_loc7_,_loc8_);
         _loc5_.scaleX = _loc5_.scaleY = _loc9_ * 0.5;
         _loc5_.x = _loc4_.x;
         _loc5_.y = _loc4_.y;
         if(param3)
         {
            (param2.getChildByName("txtName") as TextField).text = param1.tname + "";
            (param2.getChildByName("txtClass") as TextField).text = param1.cls + "";
            (param2.getChildByName("txtCpx") as TextField).text = param1.cpx + "";
            (param2.getChildByName("txtValue") as TextField).text = int(param1.value) + "";
            (param2.getChildByName("txtHp") as TextField).text = int(param1.hp) + "";
            (param2.getChildByName("txtFire") as TextField).text = param1.fire.toFixed(2) + "";
            (param2.getChildByName("txtSpeed") as TextField).text = param1.speed.toFixed(1) + "";
            (param2.getChildByName("txtTurn") as TextField).text = param1.turn.toFixed(1) + "";
            _loc10_ = false;
            _loc11_ = 0;
            while(_loc11_ < 5)
            {
               if(ConfigAndStory.AI[_loc11_] == param1.link.index)
               {
                  _loc10_ = true;
                  break;
               }
               _loc11_++;
            }
            if(!_loc10_)
            {
               if(ConfigAndStory.PLAYER == param1.link.index)
               {
                  _loc10_ = true;
               }
            }
            if(_loc10_)
            {
               param2.getChildByName("inuse").alpha = 1;
            }
            else
            {
               param2.getChildByName("inuse").alpha = 0;
            }
         }
         _loc4_.parent.addChild(_loc5_);
      }
      
      public static function cleanFlyTemp(param1:MovieClip) : void
      {
         (param1.getChildByName("txtCpx") as TextField).text = "";
         (param1.getChildByName("txtHp") as TextField).text = "";
         (param1.getChildByName("txtFire") as TextField).text = "";
         (param1.getChildByName("txtSpeed") as TextField).text = "";
         (param1.getChildByName("txtTurn") as TextField).text = "";
      }
      
      public static function addFlyTemp(param1:F4ShowData, param2:MovieClip) : void
      {
         (param2.getChildByName("txtCpx") as TextField).text = param1.cpx + "";
         (param2.getChildByName("txtHp") as TextField).text = int(param1.hp) + "";
         (param2.getChildByName("txtFire") as TextField).text = param1.fire.toFixed(2) + "";
         (param2.getChildByName("txtSpeed") as TextField).text = param1.speed.toFixed(1) + "";
         (param2.getChildByName("txtTurn") as TextField).text = param1.turn.toFixed(1) + "";
      }
   }
}

