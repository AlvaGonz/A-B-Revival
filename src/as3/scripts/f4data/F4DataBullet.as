package f4data
{
   import flash.geom.Point;
   
   public class F4DataBullet
   {
      
      public var name:String = "name";
      
      public var mcName:String = "mcname";
      
      public var hp:int = 0;
      
      public var dmg:int = 0;
      
      public var life:int = 0;
      
      public var aim:Number = 0;
      
      public var deadAct:int = 5;
      
      public var index:int = 0;
      
      public var hit:Point = new Point();
      
      public var mass:Number;
      
      public var partPoint:Point;
      
      public var btwh:Number = 0;
      
      public function F4DataBullet()
      {
         super();
      }
      
      public function readFromObj(param1:Object, param2:Boolean) : void
      {
         this.mass = param1["mass"];
         this.name = param1["name"];
         this.index = param1["index"];
         this.mcName = param1["mcName"];
         this.hp = param1["hp"];
         this.dmg = param1["dmg"];
         this.life = param1["life"];
         this.aim = param1["aim"];
         this.deadAct = param1["deadAct"];
         this.btwh = param1["btwh"];
         if(!param2)
         {
            this.hit = new Point(param1["hit"]["x"],param1["hit"]["y"]);
            if(param1["partPoint"])
            {
               this.partPoint = new Point(param1["partPoint"]["x"],param1["partPoint"]["y"]);
            }
         }
      }
      
      public function writeObj() : Object
      {
         var _loc1_:Object = new Object();
         _loc1_["name"] = this.name;
         _loc1_["mass"] = this.mass;
         _loc1_["index"] = this.index;
         _loc1_["mcName"] = this.mcName;
         _loc1_["hp"] = this.hp;
         _loc1_["dmg"] = this.dmg;
         _loc1_["life"] = this.life;
         _loc1_["aim"] = this.aim;
         _loc1_["deadAct"] = this.deadAct;
         _loc1_["hit"] = this.hit;
         _loc1_["partPoint"] = this.partPoint;
         _loc1_["btwh"] = this.btwh;
         return _loc1_;
      }
      
      public function get bulletPow() : Number
      {
         return this.dmg * (this.life + 1) + this.hp + this.deadAct + (this.hit.x + this.hit.y) * 8;
      }
   }
}

