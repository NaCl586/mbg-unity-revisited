//------------------------------------------------------------------------------
// Copyright (c) 2015 HiGuy Smith
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//  * Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
//  * Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//  * Neither the name of the project nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//------------------------------------------------------------------------------

#ifndef dif_staticMesh_h
#define dif_staticMesh_h

#include <dif/base/types.h>
#include <dif/base/io.h>

DIF_NAMESPACE

class StaticMesh : public Readable, public Writable {
public:
	struct Primitive : public Readable, public Writable {
		U8 alpha;
		U32 texS;
		U32 texT;
		S32 diffuseIndex;
		S32 lightMapIndex;
		U32 start;
		U32 count;
		PlaneF lightMapEquationX;
		PlaneF lightMapEquationY;
		glm::ivec2 lightMapOffset;
		glm::ivec2 lightMapSize;

		Primitive() : alpha(0), texS(0), texT(0), diffuseIndex(0), lightMapIndex(0), start(0), count(0) {};

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct MaterialList : public Readable, public Writable {
		U32 materialCount;

		U32 *flags;
		U32 *reflectanceMap;
		U32 *bumpMap;
		U32 *detailMap;
		U32 *lightMap;
		U32 *detailScale;
		U32 *reflectionAmount;

		MaterialList() : materialCount(0), flags(nullptr), reflectanceMap(nullptr), bumpMap(nullptr), detailMap(nullptr), lightMap(nullptr), detailScale(nullptr), reflectionAmount(nullptr) {};

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	std::vector<Primitive> primitive;
	std::vector<U16> index;
	std::vector<glm::vec3> vertex;
	std::vector<glm::vec3> normal;
	std::vector<glm::vec2> diffuseUV;
	std::vector<glm::vec2> lightmapUV;

	U8 hasMaterialList;
	MaterialList baseMaterialList;

	U32 numDiffuseBitmaps;

	U8 hasSolid;
	U8 hasTranslucency;
	BoxF bounds;
	glm::mat4 transform;
	glm::vec3 scale;

	StaticMesh() : hasMaterialList(0), numDiffuseBitmaps(0), hasSolid(0), hasTranslucency(0) {};

	/**
	 * Reads a StaticMesh from a stream
	 * @param stream The stream to read from
	 * @return If the operation was successful
	 */
	virtual bool read(std::istream &stream, Version &version);
	/**
	 * Writes a StaticMesh to a stream
	 * @param stream The stream to write to
	 * @return If the operation was successful
	 */
	virtual bool write(std::ostream &stream, Version version) const;
};

DIF_NAMESPACE_END

#endif
