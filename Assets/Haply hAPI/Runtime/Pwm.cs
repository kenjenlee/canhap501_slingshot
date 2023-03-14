using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Haply.hAPI
{
    public class Pwm
    {
        public int pin { get; set; }
        public int value { get; set; }

        /**
         * Constructs an empty PWM output for use
         */
        public Pwm ()
        {
            pin = 0;
            value = 0;
        }

        /**
         * Constructs a PWM output at the specified pin and at the desired percentage 
         * 
         *	@param	pin pin to output pwm signal
         * @param 	pulseWidth percent of pwm output, value between 0 to 100
         */
        public Pwm ( int pin, float pulseWidth )
        {
            this.pin = pin;

            if ( pulseWidth > 100.0 )
            {
                value = 255;
            }
            else
            {
                value = (int) (pulseWidth * 255 / 100);
            }
        }

        /**
         * Set value variable of pwm
         */
        public void SetPulse ( float percent )
        {
            if ( percent > 100.0 )
            {
                value = 255;
            }
            else if ( percent < 0 )
            {
                value = 0;
            }
            else
            {
                value = (int) (percent * 255 / 100);
            }
        }

        /**
         * @return percent value of pwm signal	 
         */
        public float get_pulse ()
        {
            float percent = value * 100 / 255;

            return percent;
        }
    }
}