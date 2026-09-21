FROM datalust/seq:2024.3

# Install curl
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && apt-get clean

# Set environment variable
ENV ACCEPT_EULA=Y

# Expose ports
EXPOSE 80 5341