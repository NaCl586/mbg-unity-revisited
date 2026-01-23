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

#ifndef dif_forceField_h
#define dif_forceField_h

#include <dif/base/types.h>

DIF_NAMESPACE

class ForceField : public Readable, public Writable {
public:
	struct Plane : public Readable, public Writable {
		U32 normalIndex;
		F32 planeDistance;

		Plane() : normalIndex(0), planeDistance(0.0f) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct BSPNode : public Readable, public Writable {	U16 planeIndex;
		U16 frontIndex;
		U16 backIndex;

		BSPNode() : frontIndex(0), backIndex(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct BSPSolidLeaf : public Readable, public Writable {
		U32 surfaceIndex;
		U16 surfaceCount;

		BSPSolidLeaf() : surfaceIndex(0), surfaceCount(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct Surface : public Readable, public Writable {
		U32 windingStart;
		U8 windingCount;
		U16 planeIndex;
		U8 surfaceFlags;
		U32 fanMask;

		Surface() : windingStart(0), windingCount(0), planeIndex(0), surfaceFlags(0), fanMask(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	U32 forceFieldFileVersion;
	std::string name;

	std::vector<std::string> trigger;

	BoxF boundingBox;
	SphereF boundingSphere;

	std::vector<glm::vec3> normal;
	std::vector<Plane> plane;
	std::vector<BSPNode> bspNode;
	std::vector<BSPSolidLeaf> bspSolidLeaf;
	std::vector<U32> index;
	std::vector<Surface> surface;
	std::vector<U32> solidLeafSurface;

	glm::cvec4 color;

	ForceField() : forceFieldFileVersion(0), name("") {}

	/**
	 * Reads a ForceField from a stream
	 * @param stream The stream to read from
	 * @return If the operation was successful
	 */
	virtual bool read(std::istream &stream, Version &version);
	/**
	 * Writes a ForceField to a stream
	 * @param stream The stream to write to
	 * @return If the operation was successful
	 */
	virtual bool write(std::ostream &stream, Version version) const;
};

DIF_NAMESPACE_END

#endif
