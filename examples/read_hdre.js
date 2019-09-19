
function read__HDRE(buffer)
{
	var r = HDRE.parse(buffer);

	if(!r)
	return false;

	var environments = r._envs;
	var header = r.header;
	var textures = [];

	// Create GPU Textures
	for(var i = 0; i < environments.length; i++)
	{
		var type = GL.FLOAT;			// FLOAT

		var data = environments[i].data;
		var width = environments[i].width;
		var height = environments[i].height;

		if(header.array_type == 01)		// UBYTE
			type = GL.UNSIGNED_BYTE;
		if(header.array_type == 02)		// HALF FLOAT
			type = GL.HALF_FLOAT_OES;
		if(header.array_type == 04)		// RGBE
			type = GL.UNSIGNED_BYTE;

		var options = {
			format: gl.RGBA,
			type: type,
			minFilter: gl.LINEAR_MIPMAP_LINEAR,
			texture_type: GL.TEXTURE_CUBE_MAP,
			pixel_data: data
		};

		textures.push( new Texture( width, height, options) );
	}

	// Store specular reflection (mip 0)
	gl.textures[tex_name] = textures[0];

	// Store the rest of levels
	for(var i = 1; i < 6; i++)
		gl.textures["@mip" + i + "__" + tex_name] = textures[i];

	return true;
}

function write__HDRE = function( cube_tex, options )
{
	var width = texture.width;
	var height = texture.height;
	
	var originalSkybox = this.processSkybox( temp ? temp : texture, isRGBE );
	var data = [];

	/*
		Get all mip levels from current cube_tex
		and store them so data ends up as:

		var data = [
		  // for each mip level
		  { width: w, height: h, pixelData: [ face1_array, face2_array, ..., face6_array ] },
		  .
		  .
		  .
		  { ... }
		]
	*/ 


	var write_options = {
		type: arrayType, 
		rgbe: isRGBE,
		sh: spherical_harminics_coeffs // If available (as float array[27])
	}

	var buffer = HDRE.write( data, width, height, write_options );
}