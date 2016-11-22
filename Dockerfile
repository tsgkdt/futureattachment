FROM elasticsearch:5.0.1

MAINTAINER tsgkadot <tsgkadot@gmail.com>

RUN /usr/share/elasticsearch/bin/elasticsearch-plugin install ingest-attachment && \
    chown -R elasticsearch:elasticsearch /usr/share/elasticsearch/plugins
