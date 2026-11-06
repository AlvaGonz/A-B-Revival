package f4.unit.particle
{
   import f4.data.InitData;
   import f4.data.Targets;
   import f4.data.sound32.SoundUBlt32;
   import f4.gfx.bmpdmc.BMPD2D;
   import f4.unit.ai.AIUnity;
   import f4.unit.render.Cam;
   import f4data.sdata.ConfigAndStory;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   import fullshit.ds.AbsOne;
   import fullshit.tool.Rect2;
   import fullshit.tool.StaticShit;
   
   public class Item extends AbsOne
   {
      
      public var bmpd2d:BMPD2D;
      
      public var data:Array;
      
      public var point:Point;
      
      public var spd:Point;
      
      public var life:int;
      
      public var autof:Number;
      
      public var timer:int;
      
      protected var _rect2:Rect2;
      
      public var cam:Cam;
      
      public var hit:Rectangle;
      
      public function Item(param1:Array, param2:BMPD2D, param3:Cam, param4:Number, param5:Number, param6:Number, param7:Number, param8:Number, param9:int)
      {
         super();
         this.point = new Point(param4,param5);
         this.spd = new Point(Math.cos(param6) * param7,Math.sin(param6) * param7);
         this._rect2 = StaticShit.RECT2;
         this.life = param9;
         this.autof = param8;
         this.cam = param3;
         this.data = param1;
         this.bmpd2d = param2;
         this.timer = 0;
         this.hit = new Rectangle(this.point.x,this.point.y,this.bmpd2d.bmpd.rectSmall.width * 0.8,this.bmpd2d.bmpd.rectSmall.height * 0.8);
      }
      
      override public function runA1() : void
      {
         this.point.x += this.spd.x;
         this.point.y += this.spd.y;
         this.hit.x = this.point.x - this.hit.width / 2;
         this.hit.y = this.point.y - this.hit.height / 2;
         this.spd.x *= this.autof;
         this.spd.y *= this.autof;
         this.hitWall();
         if(this.life-- <= 0)
         {
            isDeadA1 = true;
         }
      }
      
      public function beHit(param1:AIUnity) : void
      {
         var _loc2_:SoundUBlt32 = null;
         var _loc4_:int = 0;
         var _loc6_:AIUnity = null;
         isDeadA1 = true;
         var _loc3_:Number = 1;
         var _loc5_:Targets = InitData.TARGETS;
         if(this.data[0] == "fuel")
         {
            _loc4_ = 8;
         }
         else if(this.data[0] == "life")
         {
            param1.unity.autoHP = 0.009;
            param1.unity.autoHT += 36;
            for each(_loc6_ in _loc5_.pfa1.array)
            {
               _loc6_.unity.autoHP = 0.0075;
               _loc6_.unity.autoHT += 36;
            }
            _loc4_ = 10;
            _loc3_ = 1.4;
         }
         else if(this.data[0] == "money")
         {
            ConfigAndStory.ALLPART.levelMoney += this.data[1];
            _loc4_ = 9;
            _loc3_ = 0.5;
         }
         else if(this.data[0] == "part")
         {
            ConfigAndStory.ALLPART.itemPart.push(this.data[1]);
            _loc4_ = 11;
         }
         _loc2_ = new SoundUBlt32(InitData.SOUNDDATA.soundArray[_loc4_],_loc4_,new Point(this.point.x,this.point.y));
         _loc2_.volMut = _loc3_;
         InitData.SOUNDC32.addSFX25(_loc2_);
         _loc5_.evts.getItem(this.data[0]);
      }
      
      override public function startRemoveA1() : void
      {
         var _loc1_:Particle = null;
         var _loc2_:Targets = null;
         var _loc3_:int = 0;
         if(this.life > 0)
         {
            _loc2_ = InitData.TARGETS;
            _loc3_ = 0;
            while(_loc3_ < 10)
            {
               _loc1_ = new Particle(this.cam,Math.random() * this.hit.width + this.hit.x,Math.random() * this.hit.height + this.hit.y,Math.random() * Math.PI * 2,Math.random() * 0.8,1,0,0.999,Math.random() * 50 + 20);
               _loc1_.flash = true;
               _loc1_.color = 16777215;
               _loc2_.pa1.addOne1(_loc1_);
               _loc3_++;
            }
         }
      }
      
      public function render() : void
      {
         if(!isDeadA1 && this._rect2.rectIntersects(this.cam.rect,this.hit))
         {
            ++this.timer;
            this.bmpd2d.makeDrawPoint(this.point.x - this.cam.rect.x,this.point.y - this.cam.rect.y);
            this.bmpd2d.bmpd.nextFrame();
            this.bmpd2d.bmpd.moveRect();
            if(this.life > 5 * 36)
            {
               this.bmpd2d.render(0);
            }
            else if(this.timer % 5 < 2)
            {
               this.bmpd2d.render(0);
            }
         }
      }
      
      public function hitWall() : void
      {
         if(this.point.x < 0)
         {
            this.point.x = 0;
            this.spd.x = -0.1 * this.spd.x;
         }
         else if(this.point.x > this.cam.wh.x)
         {
            this.point.x = this.cam.wh.x;
            this.spd.x = -0.1 * this.spd.x;
         }
         if(this.point.y < 0)
         {
            this.point.y = 0;
            this.spd.y = -0.1 * this.spd.y;
         }
         else if(this.point.y > this.cam.wh.y)
         {
            this.point.y = this.cam.wh.y;
            this.spd.y = -0.1 * this.spd.y;
         }
      }
   }
}

