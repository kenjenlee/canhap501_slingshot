namespace Haply.hAPI
{
    public class Actuator
    {
        public int actuator { get; set; }
        public int direction { get; set; }
        public float Torque { get; set; }
        public int port { get; set; }

        /**
         * Creates an Actuator using the given motor port position
         *
         * @param	actuator actuator index
         * @param	port motor port position for actuator
         */
        public Actuator ( int actuator, int direction, int port )
        {
            this.actuator = actuator;
            this.direction = direction;
            this.port = port;
        }
    }
}