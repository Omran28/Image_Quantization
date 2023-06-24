# Image_Quantization
- An Algorithm project used image processing technique that reduces the size of an image by reducing the number of colors.
- Has multiple stages:
1.	Stored all distinct colors in the image.
2.	Calculated the distance (cost) between each node and the correct adjacent node.
3.	Correct adjacent node is calculated by choosing the least cost at a certain vertex.
4.	Choose K value that represents the number of clusters.
5.	Clusters are groups that contains least-distance between colors in this cluster.
6.	Calculate the representative color for each cluster by adding all red, green, blue values of all colors and divide by their number.
7.	Assign each pixel in the image with its representative color.
