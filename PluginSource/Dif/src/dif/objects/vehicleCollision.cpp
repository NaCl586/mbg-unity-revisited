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

#include <dif/base/io.h>
#include <dif/objects/vehicleCollision.h>

DIF_NAMESPACE

bool VehicleCollision::read(std::istream &stream, Version &version) {
	READCHECK(version.vehicleCollision.version, U32); //vehicleCollisionFileVersion
	READCHECK(vehicleConvexHull, std::vector<ConvexHull>); //vehicleConvexHull
	READCHECK(vehicleConvexHullEmitStringCharacter, std::vector<U8>); //vehicleConvexHullEmitStringCharacter
	READCHECK(vehicleHullIndex, std::vector<U32>); //vehicleHullIndex
	READCHECK(vehicleHullPlaneIndex, std::vector<U16>); //vehicleHullPlaneIndex
	READCHECK(vehicleHullEmitStringIndex, std::vector<U32>); //vehicleHullEmitStringIndex
	READCHECK(vehicleHullSurfaceIndex, std::vector<U32>); //vehicleHullSurfaceIndex
	READCHECK(vehiclePolyListPlaneIndex, std::vector<U16>); //vehiclePolyListPlaneIndex
	READCHECK(vehiclePolyListPointIndex, std::vector<U32>); //vehiclePolyListPointIndex
	READCHECK(vehiclePolyListStringCharacter, std::vector<U8>); //vehiclePolyListStringCharacter
	READCHECK(vehicleNullSurface, std::vector<NullSurface>); //vehicleNullSurface
	READCHECK(vehiclePoints, std::vector<glm::vec3>); //vehiclePoints
	READCHECK(vehiclePlanes, std::vector<PlaneF>); //vehiclePlanes
	READCHECK(vehicleWindings, std::vector<U32>); //vehicleWindings
	READCHECK(vehicleWindingIndices, std::vector<WindingIndex>); //vehicleWindingIndices

	return true;
}

bool VehicleCollision::write(std::ostream &stream, Version version) const {
	WRITECHECK(version.vehicleCollision.version, U32); //vehicleCollisionFileVersion
	WRITECHECK(vehicleConvexHull, std::vector<ConvexHull>); //vehicleConvexHull
	WRITECHECK(vehicleConvexHullEmitStringCharacter, std::vector<U8>); //vehicleConvexHullEmitStringCharacter
	WRITECHECK(vehicleHullIndex, std::vector<U32>); //vehicleHullIndex
	WRITECHECK(vehicleHullPlaneIndex, std::vector<U16>); //vehicleHullPlaneIndex
	WRITECHECK(vehicleHullEmitStringIndex, std::vector<U32>); //vehicleHullEmitStringIndex
	WRITECHECK(vehicleHullSurfaceIndex, std::vector<U32>); //vehicleHullSurfaceIndex
	WRITECHECK(vehiclePolyListPlaneIndex, std::vector<U16>); //vehiclePolyListPlaneIndex
	WRITECHECK(vehiclePolyListPointIndex, std::vector<U32>); //vehiclePolyListPointIndex
	WRITECHECK(vehiclePolyListStringCharacter, std::vector<U8>); //vehiclePolyListStringCharacter
	WRITECHECK(vehicleNullSurface, std::vector<NullSurface>); //vehicleNullSurface
	WRITECHECK(vehiclePoints, std::vector<glm::vec3>); //vehiclePoints
	WRITECHECK(vehiclePlanes, std::vector<PlaneF>); //vehiclePlanes
	WRITECHECK(vehicleWindings, std::vector<U32>); //vehicleWindings
	WRITECHECK(vehicleWindingIndices, std::vector<WindingIndex>); //vehicleWindingIndices

	return true;
}

bool VehicleCollision::ConvexHull::read(std::istream &stream, Version &version) {
	READCHECK(hullStart, U32); //hullStart
	READCHECK(hullCount, U16); //hullCount
	READCHECK(minX, F32); //minX
	READCHECK(maxX, F32); //maxX
	READCHECK(minY, F32); //minY
	READCHECK(maxY, F32); //maxY
	READCHECK(minZ, F32); //minZ
	READCHECK(maxZ, F32); //maxZ
	READCHECK(surfaceStart, U32); //surfaceStart
	READCHECK(surfaceCount, U16); //surfaceCount
	READCHECK(planeStart, U32); //planeStart
	READCHECK(polyListPlaneStart, U32); //polyListPlaneStart
	READCHECK(polyListPointStart, U32); //polyListPointStart
	READCHECK(polyListStringStart, U32); //polyListStringStart


	return true;
}

bool VehicleCollision::ConvexHull::write(std::ostream &stream, Version version) const {
	WRITECHECK(hullStart, U32); //hullStart
	WRITECHECK(hullCount, U16); //hullCount
	WRITECHECK(minX, F32); //minX
	WRITECHECK(maxX, F32); //maxX
	WRITECHECK(minY, F32); //minY
	WRITECHECK(maxY, F32); //maxY
	WRITECHECK(minZ, F32); //minZ
	WRITECHECK(maxZ, F32); //maxZ
	WRITECHECK(surfaceStart, U32); //surfaceStart
	WRITECHECK(surfaceCount, U16); //surfaceCount
	WRITECHECK(planeStart, U32); //planeStart
	WRITECHECK(polyListPlaneStart, U32); //polyListPlaneStart
	WRITECHECK(polyListPointStart, U32); //polyListPointStart
	WRITECHECK(polyListStringStart, U32); //polyListStringStart

	return true;
}

bool VehicleCollision::NullSurface::read(std::istream &stream, Version &version) {
	READCHECK(windingStart, U32); //windingStart
	READCHECK(planeIndex, U16); //planeIndex
	READCHECK(surfaceFlags, U8); //surfaceFlags
	READCHECK(windingCount, U32); //windingCount

	return true;
}

bool VehicleCollision::NullSurface::write(std::ostream &stream, Version version) const {
	WRITECHECK(windingStart, U32); //windingStart
	WRITECHECK(planeIndex, U16); //planeIndex
	WRITECHECK(surfaceFlags, U8); //surfaceFlags
	WRITECHECK(windingCount, U32); //windingCount

	return true;
}

bool VehicleCollision::WindingIndex::read(std::istream &stream, Version &version) {
	READCHECK(windingStart, U32); //windingStart
	READCHECK(windingCount, U32); //windingCount

	return true;
}

bool VehicleCollision::WindingIndex::write(std::ostream &stream, Version version) const {
	WRITECHECK(windingStart, U32); //windingStart
	WRITECHECK(windingCount, U32); //windingCount

	return true;
}

DIF_NAMESPACE_END
