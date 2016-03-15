#ifndef http_parser_interop_h
#define http_parser_interop_h

#include "http_parser.h"

struct http_parser_sizeof_types {
	size_t sizeofShort;
	size_t sizeofInt;
	size_t sizeofLong;
	size_t sizeofHttpParser;
	size_t sizeofHttpParserSettings;
};
enum HttpParserCallbackID
{
    HTTP_PARSER_ON_MESSAGE_BEGIN    = 0,
	HTTP_PARSER_ON_URL              = 1,
	HTTP_PARSER_ON_STATUS           = 2,
	HTTP_PARSER_ON_HEADER_FIELD     = 3,
	HTTP_PARSER_ON_HEADER_VALUE     = 4,
	HTTP_PARSER_ON_HEADERS_COMPLETE = 5,
	HTTP_PARSER_ON_BODY             = 6,
	HTTP_PARSER_ON_MESSAGE_COMPLETE = 7,
	HTTP_PARSER_ON_CHUNK_HEADER     = 8,
	HTTP_PARSER_ON_CHUNK_COMPLETE   = 9,
};

size_t http_parser_sizeof_size_t();

void http_parser_get_sizeof_types(struct http_parser_sizeof_types* sizeofTypes);

// Returns 0 on success
int http_parser_set_callback(http_parser_settings* settings, enum CallbackID id, void* callback);

#endif