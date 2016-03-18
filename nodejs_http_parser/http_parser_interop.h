#ifndef http_parser_interop_h
#define http_parser_interop_h

#include "http_parser.h"

struct http_parser_sizes_and_offsets {
	size_t sizeofShort;
	size_t sizeofInt;
	size_t sizeofLong;

	// http_parser
	size_t sizeofHttpParser;
	size_t offsetOfHttpParserNRead;
	size_t offsetOfHttpParserContentLength;
	size_t offsetOfHttpParserHttpMajor;
	size_t offsetOfHttpParserHttpMinor;
	size_t offsetOfHttpParserData;

	// http_parser_settings
	size_t sizeofHttpParserSettings;
	size_t offsetOfHttpParserSettingsOnMessageBegin;
	size_t offsetOfHttpParserSettingsOnUrl;
	size_t offsetOfHttpParserSettingsOnChunkComplete;
};

size_t http_parser_sizeof_size_t();

void http_parser_get_sizeof_types(struct http_parser_sizeof_types* sizeofTypes);

#endif