using UnityEngine;

namespace Haply.hAPI.Samples
{
    public class Pantograph : Mechanism
    {
        private const float GAIN = 1f;

        [SerializeField]
        private float m_Length1 = 0.07f, m_Length2 = 0.09f, m_Distance = 0.0f;

        private float m_Theta1, m_Theta2;
        private float m_Omega1, m_Omega2;
        private float m_Tau1, m_Tau2;
        private float m_fx, m_fy;
        private float m_xE, m_yE;
        private float m_vxE, m_vyE;

        private float m_JT11, m_JT12, m_JT21, m_JT22;

        public override void ForwardKinematics ( float[] angles )
        {
            // Debug.Log( "angles[0]: " + angles[0] + ", angles[1]: " + angles[1] );

            float l1 = m_Length1;
            float l2 = m_Length1;
            float L1 = m_Length2;
            float L2 = m_Length2;
            float d = m_Distance;

            m_Theta1 = Mathf.PI / 180f * angles[0];
            m_Theta2 = Mathf.PI / 180f * angles[1];

            // Forward Kinematics
            float c1 = Mathf.Cos( m_Theta1 );
            float c2 = Mathf.Cos( m_Theta2 );
            float s1 = Mathf.Sin( m_Theta1 );
            float s2 = Mathf.Sin( m_Theta2 );
            float xA = l1 * c1;
            float yA = l1 * s1;
            float xB = d + l2 * c2;

            float yB = l2 * s2;
            float hx = xB - xA;
            float hy = yB - yA;
            float hh = Mathf.Pow( hx, 2 ) + Mathf.Pow( hy, 2 );
            float hm = Mathf.Sqrt( hh );
            float cB = -(Mathf.Pow( L2, 2 ) - Mathf.Pow( L1, 2 ) - hh) / (2 * L1 * hm);

            float h1x = L1 * cB * hx / hm;
            float h1y = L1 * cB * hy / hm;
            float h1h1 = Mathf.Pow( h1x, 2 ) + Mathf.Pow( h1y, 2 );
            float h1m = Mathf.Sqrt( h1h1 );
            float sB = Mathf.Sqrt( 1 - Mathf.Pow( cB, 2 ) );

            float lx = -L1 * sB * h1y / h1m;
            float ly = L1 * sB * h1x / h1m;

            float x_P = xA + h1x + lx;
            float y_P = yA + h1y + ly;

            float phi1 = Mathf.Acos( (x_P - l1 * c1) / L1 );
            float phi2 = Mathf.Acos( (x_P - d - l2 * c2) / L2 );

            float c11 = Mathf.Cos( phi1 );
            float s11 = Mathf.Sin( phi1 );
            float c22 = Mathf.Cos( phi2 );
            float s22 = Mathf.Sin( phi2 );

            float dn = L1 * (c11 * s22 - c22 * s11);
            float eta = (-L1 * c11 * s22 + L1 * c22 * s11 - c1 * l1 * s22 + c22 * l1 * s1) / dn;
            float nu = l2 * (c2 * s22 - c22 * s2) / dn;

            m_JT11 = -L1 * eta * s11 - L1 * s11 - l1 * s1;
            m_JT12 = L1 * c11 * eta + L1 * c11 + c1 * l1;
            m_JT21 = -L1 * s11 * nu;
            m_JT22 = L1 * c11 * nu;

            m_xE = x_P;
            m_yE = y_P;

            // Debug.Log( "x_E: " + m_xE + ", y_E: " + m_yE );
        }

        public override void VelocityCalculation ( float[] angularVelocities )
        {
            m_Omega1 = Mathf.PI / 180 * angularVelocities[0];
            m_Omega2 = Mathf.PI / 180 * angularVelocities[1];

            m_vxE = m_JT11 * m_Omega1 + m_JT12 * m_Omega2;
            m_vyE = m_JT21 * m_Omega1 + m_JT22 * m_Omega2;
        }

        public override void TorqueCalculation ( float[] force )
        {
            m_fx = force[0];
            m_fy = force[1];

            m_Tau1 = m_JT11 * m_fx + m_JT12 * m_fy;
            m_Tau2 = m_JT21 * m_fx + m_JT22 * m_fy;

            m_Tau1 = m_Tau1 * GAIN;
            m_Tau2 = m_Tau2 * GAIN;
        }

        public override void ForceCalculation () { }

        public override void PositionControl ( float[] position ) { }

        public override void InverseKinematics () { }

        public override void SetMechanismParameters ( float[] parameters )
        {
            m_Length1 = parameters[0];
            m_Length2 = parameters[1];
            m_Distance = parameters[2];
        }

        public override void SetSensorData ( float[] data ) { }

        public override void GetCoordinate ( float[] buffer )
        {
            buffer[0] = m_xE;
            buffer[1] = m_yE;
        }

        public override void GetTorque ( float[] buffer )
        {
            buffer[0] = m_Tau1;
            buffer[1] = m_Tau2;
        }

        public override void GetAngle ( float[] buffer )
        {
            buffer[0] = m_Theta1;
            buffer[1] = m_Theta2;
        }

        public override void GetVelocity ( float[] buffer )
        {
            buffer[0] = m_vxE;
            buffer[1] = m_vyE;
        }

        public override void GetAngularVelocity ( float[] buffer )
        {
            buffer[0] = m_Omega1;
            buffer[1] = m_Omega2;
        }
    }
}