
function read__HDRE(buffer, flipY)
{
	var r = HDRE.parse(buffer);

	if(!r)
	return false;

	var environments = r._envs;
	var header = r.header;

	var type = GL.FLOAT;			// FLOAT

	if(header.array_type == 01)		// UBYTE
		type = GL.UNSIGNED_BYTE;
	if(header.array_type == 02)		// HALF FLOAT
		type = GL.HALF_FLOAT_OES;
	if(header.array_type == 04)		// RGBE
		type = GL.UNSIGNED_BYTE;

	// Create original environment texture

	var options = {
		format: header.nChannels === 4 ? gl.RGBA : gl.RGB,
		type: type,
		minFilter: gl.LINEAR_MIPMAP_LINEAR,
		texture_type: GL.TEXTURE_CUBE_MAP,
		pixel_data: _envs[0].data
	};

	// Flip if necessary
	gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, flipY );

	let tex = new GL.Texture( _envs[0].width, _envs[0].width, options);
	tex.mipmap_data = {};
	
	// Generate mipmap chain
	tex.bind(0);
	gl.generateMipmap(gl.TEXTURE_CUBE_MAP);
	tex.unbind();

	// Upload the rest of the info to mipmap storage
	// For each environment
	for(var i = 1; i < 6; i++)
	{
		var pixels =  _envs[i].data;
		
		// For each face
		for(var f = 0; f < 6; ++f)
			tex.uploadData( pixels[f], { no_flip: true, cubemap_face: f, mipmap_level: i}, true );

		tex.mipmap_data[i] = pixels;
	}

	gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, !flipY );

	tex.has_mipmaps = true;
	tex.data = null;

	return true;
}