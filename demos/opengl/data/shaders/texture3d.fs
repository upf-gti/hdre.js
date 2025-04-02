
varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform vec4 u_color;
uniform float u_time;

uniform float u_brightness;
uniform float u_pow;

uniform vec4 u_background;
uniform sampler3D u_texture;
uniform sampler2D u_jittering;
uniform vec3 u_local_camera_position;

/*
Basic ray-marching based volumetric renderer
*/

void main()
{
	// Compute ray origin and direction in volume space [-1,1]
    vec3 ro = v_position;
    vec3 rd = v_position - u_local_camera_position;

	vec3 rs = ro;   //Ray sample
    rd = normalize(rd) * (1.0 / 100.0);

	// Initialize cdest vec4 to store color
    vec4 cdest = vec4(0.0,0.0,0.0,0.0);

	// Use raymarching algorithm
    for(int i=0; i<1000; i++){
        vec3 absrs = abs(rs);
        if(i > 1 && (absrs.x > 1.0 || absrs.y > 1.0 || absrs.z > 1.0)) break;

        // Interpolation
		vec3 voxs = (rs + vec3(1.0))/2.0;
		float f = texture3D( u_texture, voxs ).x;

        // Classification
		//vec4 csrc = vec4(f,0.6*f,0.0,1.0);
		vec4 csrc = u_color * vec4(f,0.6*f*f,0.0,pow(f, u_pow));

        // Compositing
        csrc = vec4(csrc.xyz * csrc.w, csrc.w); //transparency, applied this way to avoid color bleeding
		cdest = csrc * (1.0 - cdest.w) + cdest; //compositing with previous value

        if(cdest.w >= 1.0) break;
        rs = rs + rd;
    }

	cdest = cdest * u_brightness;
	gl_FragColor = cdest + u_background*(1.0-cdest.w);
}
