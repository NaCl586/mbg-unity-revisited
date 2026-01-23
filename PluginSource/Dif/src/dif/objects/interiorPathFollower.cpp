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
#include <dif/objects/interiorPathFollower.h>

DIF_NAMESPACE

bool InteriorPathFollower::read(std::istream &stream, Version &version) {
	READCHECK(name, std::string); //name
	READCHECK(datablock, std::string); //datablock
	READCHECK(interiorResIndex, U32); //interiorResIndex
	READCHECK(offset, glm::vec3); //offset
	READCHECK(properties, Dictionary); //properties
	READCHECK(triggerId, std::vector<U32>); //triggerId
	READCHECK(wayPoint, std::vector<WayPoint>); //wayPoint
	READCHECK(totalMS, U32); //totalMS

	return true;
}

bool InteriorPathFollower::write(std::ostream &stream, Version version) const {
	WRITECHECK(name, std::string); //name
	WRITECHECK(datablock, std::string); //datablock
	WRITECHECK(interiorResIndex, U32); //interiorResIndex
	WRITECHECK(offset, glm::vec3); //offset
	WRITECHECK(properties, Dictionary); //properties
	WRITECHECK(triggerId, std::vector<U32>); //triggerId
	WRITECHECK(wayPoint, std::vector<WayPoint>); //wayPoint
	WRITECHECK(totalMS, U32); //totalMS

	return true;
}

bool InteriorPathFollower::WayPoint::read(std::istream &stream, Version &version) {
	READCHECK(position, glm::vec3); //position
	READCHECK(rotation, glm::quat); //rotation
	READCHECK(msToNext, U32); //msToNext
	READCHECK(smoothingType, U32); //smoothingType

	return true;
}

bool InteriorPathFollower::WayPoint::write(std::ostream &stream, Version version) const {
	WRITECHECK(position, glm::vec3); //position
	WRITECHECK(rotation, glm::quat); //rotation
	WRITECHECK(msToNext, U32); //msToNext
	WRITECHECK(smoothingType, U32); //smoothingType

	return true;
}

DIF_NAMESPACE_END
