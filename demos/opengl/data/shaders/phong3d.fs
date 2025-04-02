
//precision highp float;

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform vec4 u_color;
uniform float u_time;

uniform mat4 u_model;

uniform vec3 u_resolution;
uniform vec4 u_background;
uniform int u_blocksize;
uniform float u_upper_threshold;
uniform float u_lower_threshold;
uniform float u_minalpha;
uniform sampler3D u_texture;
uniform sampler2D u_jittering;

uniform float u_brightness;
uniform float u_pow;

uniform vec3 u_local_camera_position;
uniform vec3 u_local_light_position;

/*
Basic ray-marching based volumetric renderer
*/

// Return point where the ray enters the box. If the ray originates inside the box it returns the origin.
vec3 rayOrigin(in vec3 ro, in vec3 rd){
    if(abs(ro.x) <= 1.0 && abs(ro.y) <= 1.0 && abs(ro.z) <= 1.0) return ro;
    vec3 ip;
    // Only one these sides can hold the ray origin. The other faces will never hold it
    vec3 sides = vec3(-sign(rd.x),-sign(rd.y),-sign(rd.z));
    for(int i=0; i<3; i++){
        float c = (sides[i] - ro[i]) / rd[i];
        ip[i] = sides[i];
        ip[(i+1)%3] = c*rd[(i+1)%3]+ro[(i+1)%3];
        ip[(i+2)%3] = c*rd[(i+2)%3]+ro[(i+2)%3];
        if(abs(ip[(i+1)%3]) <= 1.0 && abs(ip[(i+2)%3]) <= 1.0) break;
    }
    return ip;
}

float sample(vec3 rs){
	return texture3D( u_texture, (rs + vec3(1.0))/2.0 ).x;
}

vec3 gradient(vec3 rs, float delta){
	vec3 s1 = vec3(sample(rs-vec3(delta, 0.0, 0.0)), sample(rs-vec3(0.0, delta, 0.0)), sample(rs-vec3(0.0, 0.0, delta)));
	vec3 s2 = vec3(sample(rs+vec3(delta, 0.0, 0.0)), sample(rs+vec3(0.0, delta, 0.0)), sample(rs+vec3(0.0, 0.0, delta)));
	return (s2-s1);
}

vec4 classify(in float f){
	return u_color * vec4(f,f,f,pow(f, u_pow));
}

vec3 shade(vec3 N, vec3 V, vec3 L, vec3 color){
	//Material, change for classify
	vec3 Ka = color;
	vec3 Kd = color;
	vec3 Ks = vec3(0.5);
	float n = 100.0;

	//Light
	vec3 lightColor = vec3(0.7);
	vec3 ambientLight = vec3(0.2);

	//Halfway vector
	vec3 H = normalize(L + V);
	
	//Ambient
	vec3 ambient = Ka * ambientLight;
	
	//Diffuse
	float diffuseLight = dot(L, N)*0.5+0.5;
	vec3 diffuse = Kd * lightColor * diffuseLight;

	//Specular
	float specularLight = pow(max(dot(H, N), 0.0), n);
	if(diffuseLight <= 0.0) specularLight = 0.0;
	vec3 specular = Ks * lightColor * specularLight;

	//return pow( 1.0 - dot(V,N), 1.0 );

	return ambient + diffuse + specular;

}

void main()
{
	// Compute ray origin and direction in volume space [-1,1]
    vec3 rd = v_position - u_local_camera_position;
	vec3 ro = rayOrigin(u_local_camera_position, rd);

	//Ray sample and direction
	rd = normalize(rd) * (1.0 / 200.0);
	ro = ro + rd*texture2D(u_jittering, gl_FragCoord.xy).x;
	vec3 rs = ro;
	vec3 nextrs = ro;
	vec3 absrs;

	//Light
	float delta = (1.0 / 100.0);
	vec3 N, L, V;

	// Initialize cdest vec4 to store color
    vec4 cdest = vec4(0.0,0.0,0.0,0.0);
	vec4 csrc;
	float f;

	// Use raymarching algorithm
    for(int i=0; i<1000; i++){
        if(i > 5 && (absrs.x > 1.0 || absrs.y > 1.0 || absrs.z > 1.0)) break;
		if(cdest.w >= 1.0) break;
		rs = nextrs;
		absrs = abs(rs);
		nextrs = rs + rd;

		// Interpolation
		f = sample(rs);

		// Gradient
		N = normalize(gradient(rs, delta));
		L = normalize(rs-u_local_light_position);
		V = normalize(rs-u_local_camera_position);

		// Classification
		csrc = classify(f);
		if(csrc.w < u_minalpha - u_lower_threshold || csrc.w > u_minalpha + u_upper_threshold) continue;
		
		// Shade
		csrc = vec4(shade(N, V, L, csrc.rgb), 1.0);
		//csrc = vec4(shade(N, V, L, N), 1.0);

		// Compositing
		csrc = vec4(csrc.xyz * csrc.w, csrc.w); //transparency, applied this way to avoid color bleeding
		cdest = csrc * (1.0 - cdest.w) + cdest; //compositing with previous value
    }

	cdest = cdest * u_brightness;
	gl_FragColor = cdest + u_background*(1.0 - cdest.w);

	//gl_FragColor = vec4(N,1.0);
}
