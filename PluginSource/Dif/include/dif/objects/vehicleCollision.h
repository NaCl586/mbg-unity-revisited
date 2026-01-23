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

#ifndef dif_vehicleCollision_h
#define dif_vehicleCollision_h

#include <dif/base/types.h>

DIF_NAMESPACE

class VehicleCollision : public Readable, public Writable {
public:
	struct ConvexHull : public Readable, public Writable {
		U32 hullStart;
		U16 hullCount;
		F32 minX;
		F32 maxX;
		F32 minY;
		F32 maxY;
		F32 minZ;
		F32 maxZ;
		U32 surfaceStart;
		U16 surfaceCount;
		U32 planeStart;
		U32 polyListPlaneStart;
		U32 polyListPointStart;
		U32 polyListStringStart;

		ConvexHull() : hullStart(0), hullCount(0), minX(0.0f), maxX(0.0f), minY(0.0f), 
							maxY(0.0f), minZ(0.0f), maxZ(0.0f), surfaceStart(0), surfaceCount(0), 
							planeStart(0), polyListPlaneStart(0), polyListPointStart(0), polyListStringStart(0) {};

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct NullSurface : public Readable, public Writable {
		U32 windingStart;
		U16 planeIndex;
		U8 surfaceFlags;
		U32 windingCount;

		NullSurface() : windingStart(0), planeIndex(0), surfaceFlags(0), windingCount(0) {};

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct WindingIndex : public Readable, public Writable {
		U32 windingStart;
		U32 windingCount;

		WindingIndex() : windingStart(0), windingCount(0) {};

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	std::vector<ConvexHull> vehicleConvexHull;
	std::vector<U8> vehicleConvexHullEmitStringCharacter;
	std::vector<U32> vehicleHullIndex;
	std::vector<U16> vehicleHullPlaneIndex;
	std::vector<U32> vehicleHullEmitStringIndex;
	std::vector<U32> vehicleHullSurfaceIndex;
	std::vector<U16> vehiclePolyListPlaneIndex;
	std::vector<U32> vehiclePolyListPointIndex;
	std::vector<U8> vehiclePolyListStringCharacter;
	std::vector<NullSurface> vehicleNullSurface;
	std::vector<glm::vec3> vehiclePoints;
	std::vector<PlaneF> vehiclePlanes;
	std::vector<U32> vehicleWindings;
	std::vector<WindingIndex> vehicleWindingIndices;

	VehicleCollision() {};

	/**
	 * Reads a VehicleCollision from a stream
	 * @param stream The stream to read from
	 * @return If the operation was successful
	 */
	virtual bool read(std::istream &stream, Version &version);
	/**
	 * Writes a VehicleCollision to a stream
	 * @param stream The stream to write to
	 * @return If the operation was successful
	 */
	virtual bool write(std::ostream &stream, Version version) const;
};

DIF_NAMESPACE_END

#endif
