package f4data
{
   import flash.geom.Point;
   
   public class F4DataOrg
   {
      
      public static const TYPE_BODY:int = 0;
      
      public static const TYPE_WING:int = 1;
      
      public static const TYPE_ENGINE:int = 2;
      
      public static const TYPE_GUN:int = 3;
      
      public static const WING_CAP_SMALL:int = 4;
      
      public static const WING_CAP_MEDIUM:int = 6;
      
      public static const WING_CAP_LARGE:int = 8;
      
      public static const WING_CAP_MEGA:int = 12;
      
      public static const WING_CAP_LEGACY:int = 10;
      
      public var type:int = 0;
      
      public var name:String = "name";
      
      public var mcName:String = "mcname";
      
      public var lr:int = 0;
      
      public var rot:int = 0;
      
      public var deadAct:String = "dead";
      
      public var maxhp:int = 0;
      
      public var powFly:Number = 0;
      
      public var turn:Number = 0;
      
      public var turnFly:Number = 0;
      
      public var mass:Number = 0;
      
      public var hit:Point;
      
      public var firePoints:Array;
      
      public var partPoint:Point;
      
      public var linkOff:Point;
      
      public var hole:Array;
      
      public var index:int = 0;
      
      public var rpindex:int = 0;
      
      public var useAs:int = 0;
      
      public var sel:int = 0;
      
      public var fire:F4DataFire;
      
      public var bullet:F4DataBullet;
      
      public var dead:Boolean;
      
      public var snew:Boolean = true;
      
      public var cost:int;
      
      public function F4DataOrg()
      {
         super();
         this.firePoints = [];
         this.partPoint = new Point(NaN,NaN);
         this.hit = new Point();
         this.fire = new F4DataFire();
         this.bullet = new F4DataBullet();
         this.linkOff = new Point();
         this.hole = [];
      }
      
      public function readFromObj(param1:Object, param2:Array, param3:Array, param4:Boolean) : void
      {
         var _loc5_:F4DataHole = null;
         var _loc6_:* = undefined;
         this.index = param1["index"];
         this.useAs = param1["useAs"];
         this.type = param1["type"];
         this.name = param1["name"];
         this.mcName = param1["mcName"];
         this.lr = param1["lr"];
         this.rot = param1["rot"] != null ? int(param1["rot"]) : 0;
         this.deadAct = param1["deadAct"];
         this.maxhp = param1["maxhp"];
         this.powFly = param1["powFly"];
         this.turn = param1["turn"];
         this.turnFly = param1["turnFly"];
         this.mass = param1["mass"];
         if(!param4)
         {
            this.linkOff = new Point(param1["linkOff"]["x"],param1["linkOff"]["y"]);
            this.firePoints = param1["firePoints"];
            this.partPoint = new Point(param1["partPoint"]["x"],param1["partPoint"]["y"]);
            this.hit = new Point(param1["hit"]["x"],param1["hit"]["y"]);
            for(_loc6_ in param1["holedata"])
            {
               _loc5_ = new F4DataHole();
               _loc5_.readFromObj(param1["holedata"][_loc6_]);
               _loc5_.parentOrg = this;
               this.hole[_loc6_] = _loc5_;
            }
            this.sel = param1["sel"];
            this.snew = param1["snew"];
            this.dead = param1["dead"];
         }
         this.bullet = param3[param1["bullet"]];
         this.fire = param2[param1["fire"]];
         this.makeCost();
      }
      
      public function writeObj() : Object
      {
         var _loc2_:F4DataHole = null;
         var _loc1_:Object = new Object();
         _loc1_["index"] = this.index;
         _loc1_["useAs"] = this.useAs;
         _loc1_["sel"] = this.sel;
         _loc1_["type"] = this.type;
         _loc1_["name"] = this.name;
         _loc1_["mcName"] = this.mcName;
         _loc1_["lr"] = this.lr;
         _loc1_["rot"] = this.rot;
         _loc1_["deadAct"] = this.deadAct;
         _loc1_["maxhp"] = this.maxhp;
         _loc1_["powFly"] = this.powFly;
         _loc1_["turn"] = this.turn;
         _loc1_["turnFly"] = this.turnFly;
         _loc1_["mass"] = this.mass;
         _loc1_["firePoints"] = this.firePoints;
         _loc1_["partPoint"] = this.partPoint;
         _loc1_["hit"] = this.hit;
         _loc1_["fire"] = this.fire.index;
         _loc1_["bullet"] = this.bullet.index;
         _loc1_["linkOff"] = this.linkOff;
         _loc1_["snew"] = this.snew;
         _loc1_["dead"] = this.dead;
         var _loc3_:int = int(this.hole.length);
         _loc1_["holedata"] = new Object();
         var _loc4_:int = 0;
         while(_loc4_ < _loc3_)
         {
            _loc2_ = this.hole[_loc4_];
            _loc1_["holedata"]["" + _loc4_] = _loc2_.writeObj();
            _loc4_++;
         }
         return _loc1_;
      }
      
      public function clone(param1:Array, param2:Array) : F4DataOrg
      {
         var _loc3_:Object = this.writeObj();
         var _loc4_:F4DataOrg = new F4DataOrg();
         _loc4_.readFromObj(_loc3_,param1,param2,false);
         return _loc4_;
      }
      
      public function makeCost() : void
      {
         var _loc8_:F4DataHole = null;
         var _loc1_:Number = this.maxhp * 24;
         var _loc2_:Number = this.turnFly / this.mass;
         var _loc3_:Number = this.powFly / this.mass;
         var _loc4_:int = int(this.firePoints.length);
         var _loc5_:Number = this.fire.firePowRate * 8;
         var _loc6_:Number = this.bullet.bulletPow;
         var _loc7_:Number = 1;
         for each(_loc8_ in this.hole)
         {
            if(_loc8_.canWing())
            {
               _loc7_ += 1.25;
            }
            else
            {
               _loc7_ += 0.55;
            }
         }
         _loc7_ += _loc2_ * 74;
         _loc7_ = _loc7_ + _loc3_ * 54;
         if(isNaN(_loc5_))
         {
            _loc5_ = 0;
         }
         if(this.type == 0)
         {
            _loc5_ *= 0.11;
         }
         this.cost = Math.ceil((_loc1_ + _loc4_ * (_loc5_ * _loc6_) / 8) * _loc7_ * 0.0065);
      }
      
      public static function wingCategoryForHull(param1:F4DataOrg) : String
      {
         if(!param1 || !param1.name)
         {
            return "Unknown";
         }
         if(param1.name.indexOf("B.Small") == 0)
         {
            return "Small";
         }
         // B.Med++ hulls do not match "B.Medium"
         if(param1.name.indexOf("B.Med") == 0)
         {
            return "Medium";
         }
         if(param1.name.indexOf("B.Large") == 0)
         {
            return "Large";
         }
         if(param1.name.indexOf("B.Mega") == 0)
         {
            return "Mega";
         }
         return "Unknown";
      }
      
      public static function maxWingsForHull(param1:F4DataOrg) : int
      {
         var _loc2_:String = wingCategoryForHull(param1);
         if(_loc2_ == "Small")
         {
            return WING_CAP_SMALL;
         }
         if(_loc2_ == "Medium")
         {
            return WING_CAP_MEDIUM;
         }
         if(_loc2_ == "Large")
         {
            return WING_CAP_LARGE;
         }
         if(_loc2_ == "Mega")
         {
            return WING_CAP_MEGA;
         }
         return WING_CAP_LEGACY;
      }
   }
}

