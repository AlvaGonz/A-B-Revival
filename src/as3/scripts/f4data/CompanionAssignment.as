package f4data
{
   /**
    * Structured companion assignment record representing a deployed AI wingman craft,
    * its pre-battle assigned tactical role, and its assigned squad leader.
    */
   public class CompanionAssignment
   {
      public static const LEADER_MICHAEL:int = 0;
      public static const LEADER_CAPTAIN:int = 1;

      public static const ROLE_ANCHOR:int = 0;
      public static const ROLE_ESCORT:int = 1;
      public static const ROLE_EVADE:int = 2;
      public static const ROLE_SEEK:int = 3;
      public static const ROLE_SUPPORT:int = 4;
      public static const ROLE_SHIELD:int = 5;

      public static const ROLE_NAMES:Array = [
         "ANCHOR",
         "ESCORT",
         "EVADE",
         "HUNT",
         "SUPPORT",
         "SHIELD"
      ];

      public static const LEADER_NAMES:Array = [
         "MICHAEL",
         "CAPTAIN"
      ];

      public var craftIndex:int;
      public var role:int;
      public var leader:int;

      public function CompanionAssignment(param1:int = -1, param2:int = 1, param3:int = 0)
      {
         super();
         this.craftIndex = param1;
         this.role = param2 >= 0 && param2 <= 5 ? param2 : ROLE_ESCORT;
         this.leader = param3 == LEADER_CAPTAIN ? LEADER_CAPTAIN : LEADER_MICHAEL;
      }

      public function clone() : CompanionAssignment
      {
         return new CompanionAssignment(this.craftIndex, this.role, this.leader);
      }

      public function toObject() : Object
      {
         return {
            "craftIndex": this.craftIndex,
            "role": this.role,
            "leader": this.leader
         };
      }

      public static function fromObject(param1:Object) : CompanionAssignment
      {
         if(!param1)
         {
            return null;
         }
         var craft:int = param1.craftIndex != null ? int(param1.craftIndex) : -1;
         var r:int = param1.role != null ? int(param1.role) : ROLE_ESCORT;
         var l:int = param1.leader != null ? int(param1.leader) : LEADER_MICHAEL;
         return new CompanionAssignment(craft, r, l);
      }
   }
}
