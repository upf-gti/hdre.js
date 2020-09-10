/*
*   Alex Rodriguez
*   @jxarco 
*/

// hdre.js 

//main namespace
(function(global){

	/**
	 * Main namespace
	 * @namespace HDRE
	 */
	
	var FLO2BYTE = 4;
	var BYTE2BITS = 8;

	var U_BYTE		= 01;
	var HALF_FLOAT	= 02;
	var FLOAT		= 03;
	var U_BYTE_RGBE	= 04;

	var ARRAY_TYPES = {
		1: Uint8Array,
		2: Uint16Array,
		3: Float32Array,
		4: Uint8Array
	}

	var HDRE = global.HDRE = {

		version: 3.0,	// v1.5 adds spherical harmonics coeffs for the skybox
						// v2.0 adds byte padding for C++ uses				
						// v2.5 allows mip levels to be smaller than 8x8
						// v2.75 RGB format supported
						// v3.0 HDREImage and more actions 
		maxFileSize: 60e6 // bytes
	};

	// En la v1.4 poner maxFileSize a 58 000 000 (bytes)
	
	HDRE.setup = function(o)
	{
		o = o || {};
		if(HDRE.configuration)
			throw("setup already called");
		HDRE.configuration = o;
	}
	
	// Uint8Array -> UInt8 -> 8 bits per element -> 1 byte
	// Float32Array -> Float32 -> 32 bits per element -> 4 bytes
	// Float64Array -> Float64 -> 64 bits per element -> 8 bytes
	
	/** HEADER STRUCTURE (256 bytes)
	 * Header signature ("HDRE" in ASCII)	   4 bytes
	 * Format Version						   4 bytes
	 * Width									2 bytes
	 * Height								   2 bytes
	 * Max file size							4 bytes
	 * Number of channels					   1 byte
	 * Bits per channel						 1 byte
	 * Header size								1 byte
	 * Max luminance							4 byte
	 * Flags									1 byte
	 */
	
	/**
	* This class stores all the HDRE data
	* @class HDREImage
	*/

	function HDREImage(header, data, o) {

		if(this.constructor !== HDREImage)
		throw("You must use new to create a HDREImage");

		this._ctor(header, data);

		if(o)
		this.configure(o);
		
	}

	HDREImage.prototype._ctor = function(h, data) {

		if(!h)
			throw("missing info");

		// file info
		this.version = h["version"];

		// dimensions
		this.width = h["width"];
		this.height = h["height"];

		// channel info
		this.n_channels = h["nChannels"];
		this.bits_channel = h["bpChannel"];
		
		// image info
		this.data = data;
		this.type = ARRAY_TYPES[h["type"]];
		this.max_irradiance = h["maxIrradiance"];
		this.shs = h["shs"];
		this.size = h["size"];

		// store h just in case
		this.header = h;

		console.log(this);
	}
	
	HDREImage.prototype.configure = function(o) {

		o = o || {};

		this.is_rgbe = o.rgbe !== undefined ? o.rgbe : false;
	}

	HDREImage.prototype.FlipY = function() {

	}

	HDREImage.prototype.ToTexture = function() {
		
		if(!window.GL)
			throw("this function requires to use litegl.js");

		var _envs = this.data;
		if(!_envs)
			return false;

		// Get base enviroment texture
		var tex_type = GL.FLOAT;
		var data = _envs[0].data;
		var flip_Y_sides = true;

		if(this.type === Uint16Array) // HALF FLOAT
			tex_type = GL.HALF_FLOAT_OES;
		else if(this.type === Uint8Array) 
			tex_type = GL.UNSIGNED_BYTE;

		if(flip_Y_sides)
		{
			var tmp = data[2];
			data[2] = data[3];
			data[3] = tmp;
		}

		var options = {
			format: this.n_channels === 4 ? gl.RGBA : gl.RGB,
			type: tex_type,
			minFilter: gl.LINEAR_MIPMAP_LINEAR,
			texture_type: GL.TEXTURE_CUBE_MAP,
			no_flip: !flip_Y_sides
		};

		var w = this.width;

		GL.Texture.disable_deprecated = true;

		var tex = new GL.Texture( w, w, options );
		tex.mipmap_data = {};

		// Generate mipmaps
		tex.bind(0);

		var num_mipmaps = Math.log(w) / Math.log(2);

		// Upload prefilter mipmaps
		for(var i = 0; i <= num_mipmaps; i++)
		{
			var level_info = _envs[i];
			var levelsize = Math.pow(2,num_mipmaps - i);

			if(level_info)
			{
				var pixels = level_info.data;
				if(flip_Y_sides && i > 0)
				{
					var tmp = pixels[2];
					pixels[2] = pixels[3];
					pixels[3] = tmp;
				}
				for(var f = 0; f < 6; ++f)
				{
					tex.uploadData( pixels[f], { no_flip: !flip_Y_sides, cubemap_face: f, mipmap_level: i}, true );
				}
				tex.mipmap_data[i] = pixels;
			}
			else
			{
				var zero = new Float32Array(levelsize * levelsize * this.n_channels);
				for(var f = 0; f < 6; ++f)
					tex.uploadData( zero, { no_flip: !flip_Y_sides, cubemap_face: f, mipmap_level: i}, true );
			}
		}

		GL.Texture.disable_deprecated = false;

		// Store the texture 
		tex.has_mipmaps = true;
		tex.data = null;
		tex.is_rgbe = this.is_rgbe;

		return tex;
	}

	/**
	* This class creates HDRE from different sources
	* @class HDREBuilder
	*/

	function HDREBuilder(o) {

		if(this.constructor !== HDREBuilder)
		throw("You must use new to create a HDREBuilder");

		this._ctor();

		if(o)
		this.configure(o);
	}
	
	HDREBuilder.prototype._ctor = function() {

	}

	HDREBuilder.prototype.configure = function(o) {

		o = o || {};

	}

	/**
	* Write and download an HDRE
	* @method write
	* @param {Object} mips_data - [lvl0: { w, h, pixeldata: [faces] }, lvl1: ...]
	* @param {Number} width
	* @param {Number} height
	* @param {Object} options
	*/
	HDRE.write = function( mips_data, width, height, options )
	{
		options = options || {};

		var array_type = Float32Array;
		
		if(options.type && options.type.BYTES_PER_ELEMENT)
			array_type = options.type;

		var RGBE = options.rgbe !== undefined ? options.rgbe : false;

		/*
		*   Create header
		*/

		// get total pixels
		var size = 0;
		for(var i = 0; i < mips_data.length; i++)
			size += mips_data[i].width * mips_data[i].height;

		// File format information
		var numFaces = 6;
		var numChannels = options.channels || 4;
		var headerSize = 256; // Bytes (256 in v2.0)
		var contentSize = size * numFaces * numChannels * array_type.BYTES_PER_ELEMENT; // Bytes
		var fileSize = headerSize + contentSize; // Bytes
		var bpChannel = array_type.BYTES_PER_ELEMENT * BYTE2BITS; // Bits

		var contentBuffer = new ArrayBuffer(fileSize);
		var view = new DataView(contentBuffer);

		var LE = true;// little endian

		// Signature: "HDRE" in ASCII
		// 72, 68, 82, 69

		// Set 4 bytes of the signature
		view.setUint8(0, 72);
		view.setUint8(1, 68);
		view.setUint8(2, 82);
		view.setUint8(3, 69);
		
		// Set 4 bytes of version
		view.setFloat32(4, this.version, LE);

		// Set 2 bytes of width, height
		view.setUint16(8, width, LE);
		view.setUint16(10, height, LE);
		// Set max file size
		view.setFloat32(12, this.maxFileSize, LE);

		// Set rest of the bytes
		view.setUint16(16, numChannels, LE); // Number of channels
		view.setUint16(18, bpChannel, LE); // Bits per channel
		view.setUint16(20, headerSize, LE); // max header size
		view.setUint16(22, LE ? 1 : 0, LE); // endian encoding

		/*
		*   Create data
		*/
		
		var data = new array_type(size * numFaces * numChannels);
		var offset = 0;

		for(var i = 0; i < mips_data.length; i++)
		{
			let _env = mips_data[i],
				w = _env.width,
				h = _env.height,
				s = w * h * numChannels;

			var suboff = 0;

			for(var f = 0; f < numFaces; f++) {
				var subdata = _env.pixelData[f];

				// remove alpha channel to save storage
				if(numChannels === 3)
					subdata = _removeAlphaChannel( subdata );

				data.set( subdata, offset + suboff);
				suboff += subdata.length;
			}

			// Apply offset
			offset += (s * numFaces);
		}

		// set max value for luminance
		view.setFloat32(24, _getMax( data ), LE); 

		var type = FLOAT;
		if( array_type === Uint8Array)
			type = U_BYTE;
		if( array_type === Uint16Array)
			type = HALF_FLOAT;

		if(RGBE)
			type = U_BYTE_RGBE;
			
		// set write array type 
		view.setUint16(28, type, LE); 

		// SH COEFFS
		if(options.sh) {
		
			var SH = options.sh;

			view.setUint16(30, 1, LE);
			view.setFloat32(32, SH.length / 3, LE); // number of coeffs
			var pos = 36;
			for(var i = 0; i < SH.length; i++) {
				view.setFloat32(pos, SH[i], LE); 
				pos += 4;
			}
		}
		else
			view.setUint16(30, 0, LE);

		/*
		*  END OF HEADER
		*/

		offset = headerSize;

		// Set data into the content buffer
		for(var i = 0; i < data.length; i++)
		{
			if(type == U_BYTE || type == U_BYTE_RGBE) {
				view.setUint8(offset, data[i]);
			}else if(type == HALF_FLOAT) {
				view.setUint16(offset, data[i], true);
			}else {
				view.setFloat32(offset, data[i], true);
			}

			offset += array_type.BYTES_PER_ELEMENT;
		}

		// Return the ArrayBuffer with the content created
		return contentBuffer;
	}

	function _getMax(data) {
		return data.reduce((max, p) => p > max ? p : max, data[0]);
	}

	function _removeAlphaChannel(data) {
		var tmp_data = new Float32Array(data.length * 0.75);
		var index = k = 0;
		data.forEach( function(a, b){  
			if(index < 3) {
				tmp_data[k++] = a;  
				index++;
			} else {
				index = 0;
			}
		});
		return tmp_data;
	}

	window.getMaxOfArray = _getMax;

	/**
	* Read file
	* @method read
	* @param {String} file 
	*/
	HDRE.load = function( url, callback )
	{
		var xhr = new XMLHttpRequest();
		xhr.responseType = "arraybuffer";
		xhr.open( "GET", url, true );
		xhr.onload = (e) => {
		if(e.target.status == 404)
			return;
		var data = HDRE.parse(e.target.response);
		if(callback)
			callback(data);
		}
		xhr.send();
	}
	
	//legacy
	HDRE.read = function( url, callback )
	{
	   console.warn("Legacy function, use HDRE.load instead of HDRE.read");
	   return HDRE.load( url, callback );
	}

	/**
	* Parse the input data and create texture
	* @method parse
	* @param {ArrayBuffer} buffer 
	* @param {Function} options (oncomplete, onprogress, filename, ...)
	*/
	HDRE.parse = function( buffer, options )
	{
		if(!buffer)
			throw( "No data buffer" );

		var options = options || {};
		var fileSizeInBytes = buffer.byteLength;
		var LE = true;

		/*
		*   Read header
		*/

		// Read signature
		var sg = parseSignature( buffer, 0 );

		// Read version
		var v = parseFloat32(buffer, 4, LE);

		// Get 2 bytes of width, height
		var w = parseUint16(buffer, 8, LE);
		var h = parseUint16(buffer, 10, LE);
		// Get max file size in bytes
		var m = parseFloat(parseFloat32(buffer, 12, LE));

		// Set rest of the bytes
		var c = parseUint16(buffer, 16, LE);
		var b = parseUint16(buffer, 18, LE);
		var s = parseUint16(buffer, 20, LE);
		var isLE = parseUint16(buffer, 22, LE);

		var i = parseFloat(parseFloat32(buffer, 24, LE));
		var a = parseUint16(buffer, 28, LE);

		var shs = null;
		var hasSH = parseUint16(buffer, 30, LE);

		if(hasSH) {
			var Ncoeffs = parseFloat32(buffer, 32, LE) * 3;
			shs = [];
			var pos = 36;

			for(var i = 0; i < Ncoeffs; i++)  {
				shs.push( parseFloat32(buffer, pos, LE) );
				pos += 4;
			}
		}

		var header = {
			version: v,
			signature: sg,
			type: a,
			width: w,
			height: h,
			nChannels: c,
			bpChannel: b,
			maxIrradiance: i,
			shs: shs,
			encoding: isLE,
			size: fileSizeInBytes
		};

		// console.table(header);
		window.parsedFile = HDRE.last_parsed_file = { buffer: buffer, header: header };
		
		if(v < 2 || v > 1e3){ // bad encoding
			console.error('old version, please update the HDRE');
			return false;
		}
		if(fileSizeInBytes > m){
			console.error('file too big');
			return false;
		}


		/*
		*   BEGIN READING DATA
		*/

		var dataBuffer = buffer.slice(s);
		var array_type = ARRAY_TYPES[header.type];

		var dataSize = dataBuffer.byteLength / 4;
		var data = new array_type(dataSize);
		var view = new DataView(dataBuffer);
		
		var pos = 0;

		for(var i = 0 ; i < dataSize; i++)
		{
			data[i] = view.getFloat32(pos, LE);
			pos += 4;
		}

		var numChannels = c;

		var ems = [],
			precomputed = [];

		var offset = 0;
		var originalWidth = w;

		for(var i = 0; i < 6; i++)
		{
			var mip_level = i + 1;
			var offsetEnd = w * w * numChannels * 6;
			ems.push( data.slice(offset, offset + offsetEnd) );
			offset += offsetEnd;
		
			if(v > 2.0)
				w = originalWidth / Math.pow(2, mip_level);
			else
				w = Math.max(8, originalWidth / Math.pow(2, mip_level));
		}

		/*
			Get bytes
		*/
		
		// care about new sizes (mip map chain)
		w = header.width;

		for(var i = 0; i < 6; i++)
		{
			var bytes = ems[i];
		
			// Reorder faces
			var faces = [];
			var bPerFace = bytes.length / 6;

			var offset = 0;

			for(var j = 0; j < 6; j++)
			{
				faces[j] = new array_type(bPerFace);

				var subdata = bytes.slice(offset, offset + (numChannels * w * w));
				faces[j].set(subdata);

				offset += (numChannels * w * w);
			}

			precomputed.push( {
				data: faces,
				width: w
			});

			// resize next textures
			var mip_level = i + 1;
			
			if(v > 2.0)
				w = originalWidth / Math.pow(2, mip_level);
			else
				w = Math.max(8, originalWidth / Math.pow(2, mip_level));

			if(options.onprogress)
				options.onprogress( i );
		}

		var image = new HDREImage(header, precomputed, options);
		return image;
	}

	// Shader Code

	//Read environment mips
	HDRE.read_cubemap_fs = '\
		vec3 readPrefilteredCube(samplerCube cube_texture, float roughness, vec3 R) {\n\
			float f = roughness * 5.0;\n\
			vec3 color = textureCubeLodEXT(cube_texture, R, f).rgb;\n\
			return color;\n\
		}\n\
	';

	//Show SHs
	HDRE.irradiance_shs_fs = '\
		varying vec3 v_normal;\n\
		uniform vec3 u_sh_coeffs[9];\n\
		\n\
		const float Pi = 3.141592654;\n\
		const float CosineA0 = Pi;\n\
		const float CosineA1 = (2.0 * Pi) / 3.0;\n\
		const float CosineA2 = Pi * 0.25;\n\
		\n\
		struct SH9 { float c[9]; };\n\
		struct SH9Color { vec3 c[9]; };\n\
		\n\
		void SHCosineLobe(in vec3 dir, out SH9 sh)\n\
		{\n\
			// Band 0\n\
			sh.c[0] = 0.282095 * CosineA0;\n\
			\n\
			// Band 1\n\
			sh.c[1] = 0.488603 * dir.y * CosineA1;\n\
			sh.c[2] = 0.488603 * dir.z * CosineA1;\n\
			sh.c[3] = 0.488603 * dir.x * CosineA1;\n\
			\n\
			sh.c[4] = 1.092548 * dir.x * dir.y * CosineA2;\n\
			sh.c[5] = 1.092548 * dir.y * dir.z * CosineA2;\n\
			sh.c[6] = 0.315392 * (3.0 * dir.z * dir.z - 1.0) * CosineA2;\n\
			sh.c[7] = 1.092548 * dir.x * dir.z * CosineA2;\n\
			sh.c[8] = 0.546274 * (dir.x * dir.x - dir.y * dir.y) * CosineA2;\n\
		}\n\
		\n\
		vec3 ComputeSHDiffuse(in vec3 normal)\n\
		{\n\
			SH9Color shs;\n\
			for(int i = 0; i < 9; ++i)\n\
				shs.c[i] = u_sh_coeffs[i];\n\
			\n\
			// Compute the cosine lobe in SH, oriented about the normal direction\n\
			SH9 shCosine;\n\
			SHCosineLobe(normal, shCosine);\n\
			\n\
			// Compute the SH dot product to get irradiance\n\
			vec3 irradiance = vec3(0.0);\n\
			const int num = 9;\n\
			for(int i = 0; i < num; ++i)\n\
				irradiance += radiance.c[i] * shCosine.c[i];\n\
			\n\
			vec3 shDiffuse = irradiance * (1.0 / Pi);\n\
			\n\
			return irradiance;\n\
		}\n\
	';

	/* 
		Private library methods
	*/
	function parseSignature( buffer, offset ) {

		var uintBuffer = new Uint8Array( buffer );
		var endOffset = 4;

		return window.TextDecoder !== undefined ? new TextDecoder().decode(new Uint8Array( buffer ).slice( offset, offset + endOffset )) : "";
	}

	function parseString( buffer, offset ) {

		var uintBuffer = new Uint8Array( buffer );
		var endOffset = 0;

		while ( uintBuffer[ offset + endOffset ] != 0 ) 
			endOffset += 1;

		return window.TextDecoder !== undefined ? new TextDecoder().decode(new Uint8Array( buffer ).slice( offset, offset + endOffset )) : "";
	}

	function parseFloat32( buffer, offset, LE ) {
	
		var Float32 = new DataView( buffer.slice( offset, offset + 4 ) ).getFloat32( 0, LE );
		return Float32;
	}

	function parseUint16( buffer, offset, LE ) {
	
		var Uint16 = new DataView( buffer.slice( offset, offset + 2 ) ).getUint16( 0, LE );
		return Uint16;
	}

	function parseUint8( buffer, offset ) {
	
		var Uint8 = new DataView( buffer.slice( offset, offset + 1 ) ).getUint8( 0 );
		return Uint8;
	}
	
//footer
})( typeof(window) != "undefined" ? window : (typeof(self) != "undefined" ? self : global ) );
	
