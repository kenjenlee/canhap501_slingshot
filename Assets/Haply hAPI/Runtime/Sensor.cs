namespace Haply.hAPI
{
    public class Sensor
    {
        public int encoder { get; set; }
        public int direction { get; set; }
        public float encoderOffset { get; set; }
        public float encoderResolution { get; set; }
        public float value { get; set; }
        public float velocity { get; set; }
        public int port { get; set; }

        /**
         * Constructs a Sensor set using motor port position one
         */
        public Sensor ()
        {
            encoder = 0;
            direction = 0;
            encoderOffset = 0.0f;
            encoderResolution = 0.0f;
            port = 0;
        }

        /**
         * Constructs a Sensor with the given motor port position, to be initialized with the given angular offset,
         * at the specified step resoluiton (used for construction of encoder sensor)
         *
         * @param	encoder encoder index
         * @param	offset initial offset in degrees that the encoder sensor should be initialized at
         * @param	resolution step resolution of the encoder sensor
         * @param	port specific motor port the encoder sensor is connect at (usually same as actuator)
         */
        public Sensor ( int encoder, int direction, float encoderOffset, float encoderResolution, int port )
        {
            this.encoder = encoder;
            this.direction = direction;
            this.encoderOffset = encoderOffset;
            this.encoderResolution = encoderResolution;
            this.port = port;
        }
    }
}